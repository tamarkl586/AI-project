using Cassandra;
using DrawReportService.DAL;
using DrawReportService.Options;
using MassTransit;
using Microsoft.Extensions.Options;
using TicketingService.Contracts;

namespace DrawReportService.Consumers;

public class TicketPurchaseInitiatedConsumer : IConsumer<TicketPurchaseInitiatedEvent>
{
    private readonly Cassandra.ISession _session;
    private readonly CassandraOptions _options;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TicketPurchaseInitiatedConsumer> _logger;

    public TicketPurchaseInitiatedConsumer(
        ICassandraSessionFactory sessionFactory,
        IOptions<CassandraOptions> options,
        IPublishEndpoint publishEndpoint,
        ILogger<TicketPurchaseInitiatedConsumer> logger)
    {
        _session = sessionFactory.Session;
        _options = options.Value;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketPurchaseInitiatedEvent> context)
    {
        var message = context.Message;

        // Attach the message's CorrelationId to every log statement in this consumer
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", message.CorrelationId.ToString()))
        {
            var winnerRowSet = await _session.ExecuteAsync(new SimpleStatement(
                $"SELECT winner_id FROM {_options.Keyspace}.winner_by_gift WHERE gift_id = ?;",
                message.GiftId));

            if (winnerRowSet.FirstOrDefault() is { } winnerRow && !winnerRow.IsNull("winner_id"))
            {
                await _publishEndpoint.Publish(new PrizeDrawingClosedEvent
                {
                    PurchaseRequestId = message.PurchaseRequestId,
                    CorrelationId = message.CorrelationId,
                    GiftId = message.GiftId,
                    Reason = "Prize drawing is already closed/completed.",
                    RejectedAtUtc = DateTime.UtcNow
                });

                _logger.LogWarning(
                    "Rejected purchase request {RequestId} for gift {GiftId}. Draw already closed.",
                    message.PurchaseRequestId,
                    message.GiftId);

                return;
            }

            await UpsertGiftPurchaseAsync(message);
            await UpsertGiftSummaryAsync(message);
            await UpsertPurchaserAnalyticsAsync(message);
            await InsertPurchaserItemAsync(message);

            await _publishEndpoint.Publish(new PrizeDrawingVerifiedEvent
            {
                PurchaseRequestId = message.PurchaseRequestId,
                CorrelationId = message.CorrelationId,
                GiftId = message.GiftId,
                VerifiedAtUtc = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Verified purchase request {RequestId} for gift {GiftId}.",
                message.PurchaseRequestId,
                message.GiftId);
        }
    }

    private async Task UpsertGiftPurchaseAsync(TicketPurchaseInitiatedEvent message)
    {
        var existingRow = (await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT quantity FROM {_options.Keyspace}.gift_purchases_by_gift WHERE gift_id = ? AND user_id = ?;",
            message.GiftId,
            message.UserId))).FirstOrDefault();

        var currentQuantity = existingRow == null || existingRow.IsNull("quantity")
            ? 0
            : existingRow.GetValue<int>("quantity");

        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.gift_purchases_by_gift
               (gift_id, user_id, user_name, user_email, quantity)
               VALUES (?, ?, ?, ?, ?);",
            message.GiftId,
            message.UserId,
            message.UserName,
            message.UserEmail,
            currentQuantity + message.Quantity));
    }

    private async Task UpsertGiftSummaryAsync(TicketPurchaseInitiatedEvent message)
    {
        var existingRow = (await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT total_tickets, total_earned, winner_name FROM {_options.Keyspace}.gift_summary_by_gift WHERE gift_id = ?;",
            message.GiftId))).FirstOrDefault();

        var currentTickets = existingRow == null || existingRow.IsNull("total_tickets")
            ? 0
            : existingRow.GetValue<int>("total_tickets");

        var currentEarned = existingRow == null || existingRow.IsNull("total_earned")
            ? 0m
            : existingRow.GetValue<decimal>("total_earned");

        var winnerName = existingRow == null || existingRow.IsNull("winner_name")
            ? string.Empty
            : existingRow.GetValue<string>("winner_name");

        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.gift_summary_by_gift
               (gift_id, gift_name, picture, price, total_tickets, total_earned, winner_name)
               VALUES (?, ?, ?, ?, ?, ?, ?);",
            message.GiftId,
            message.GiftName,
            message.GiftPicture,
            message.UnitPrice,
            currentTickets + message.Quantity,
            currentEarned + (message.Quantity * message.UnitPrice),
            winnerName));
    }

    private async Task UpsertPurchaserAnalyticsAsync(TicketPurchaseInitiatedEvent message)
    {
        var existingRow = (await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT name, email, phone, total_tickets, grand_total_spent FROM {_options.Keyspace}.purchaser_analytics_by_user WHERE user_id = ?;",
            message.UserId))).FirstOrDefault();

        var currentTickets = existingRow == null || existingRow.IsNull("total_tickets")
            ? 0
            : existingRow.GetValue<int>("total_tickets");

        var currentSpent = existingRow == null || existingRow.IsNull("grand_total_spent")
            ? 0
            : existingRow.GetValue<int>("grand_total_spent");

        var name = existingRow == null || existingRow.IsNull("name") || string.IsNullOrWhiteSpace(existingRow.GetValue<string>("name"))
            ? message.UserName
            : existingRow.GetValue<string>("name");

        var email = existingRow == null || existingRow.IsNull("email") || string.IsNullOrWhiteSpace(existingRow.GetValue<string>("email"))
            ? message.UserEmail
            : existingRow.GetValue<string>("email");

        var phone = existingRow == null || existingRow.IsNull("phone")
            ? string.Empty
            : existingRow.GetValue<string>("phone");

        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.purchaser_analytics_by_user
               (user_id, name, email, phone, total_tickets, grand_total_spent)
               VALUES (?, ?, ?, ?, ?, ?);",
            message.UserId,
            name,
            email,
            phone,
            currentTickets + message.Quantity,
            currentSpent + (message.Quantity * message.UnitPrice)));
    }

    private async Task InsertPurchaserItemAsync(TicketPurchaseInitiatedEvent message)
    {
        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.purchaser_items_by_user
               (user_id, purchased_at, gift_name, quantity, price_per_unit, total_price)
               VALUES (?, ?, ?, ?, ?, ?);",
            message.UserId,
            DateTime.UtcNow,
            message.GiftName,
            message.Quantity,
            message.UnitPrice,
            message.Quantity * message.UnitPrice));
    }
}
