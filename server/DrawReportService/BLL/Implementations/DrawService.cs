using Cassandra;
using DrawReportService.BLL.Interfaces;
using DrawReportService.DAL;
using DrawReportService.DTOs.Report;
using DrawReportService.Options;
using Microsoft.Extensions.Options;

namespace DrawReportService.BLL.Implementations;

public class DrawService : IDrawService
{
    private readonly Cassandra.ISession _session;
    private readonly CassandraOptions _options;
    private readonly IEmailService _emailService;
    private readonly ILogger<DrawService> _logger;

    public DrawService(
        ICassandraSessionFactory sessionFactory,
        IOptions<CassandraOptions> options,
        IEmailService emailService,
        ILogger<DrawService> logger)
    {
        _session = sessionFactory.Session;
        _options = options.Value;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DrawingResultDTO> DrawWinnerAsync(int giftId)
    {
        _logger.LogInformation("Draw process started for Gift ID {GiftId}", giftId);

        var existingWinner = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT winner_id FROM {_options.Keyspace}.winner_by_gift WHERE gift_id = ?;",
            giftId));

        if (existingWinner.FirstOrDefault() is { } winnerRow && !winnerRow.IsNull("winner_id"))
        {
            throw new InvalidOperationException("הגרלה כבר בוצעה למתנה זו");
        }

        var summaryRowSet = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT gift_name, picture, total_tickets FROM {_options.Keyspace}.gift_summary_by_gift WHERE gift_id = ?;",
            giftId));

        var summaryRow = summaryRowSet.FirstOrDefault();
        if (summaryRow == null)
        {
            throw new KeyNotFoundException("מתנה לא נמצאה");
        }

        var giftName = GetString(summaryRow, "gift_name");
        var picture = GetString(summaryRow, "picture");
        var totalTickets = summaryRow.IsNull("total_tickets") ? 0 : summaryRow.GetValue<int>("total_tickets");

        var purchaseRows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT user_id, user_name, user_email, quantity FROM {_options.Keyspace}.gift_purchases_by_gift WHERE gift_id = ?;",
            giftId));

        var participants = purchaseRows
            .Select(row => new
            {
                UserId = row.GetValue<int>("user_id"),
                UserName = GetString(row, "user_name"),
                UserEmail = GetString(row, "user_email"),
                Quantity = row.IsNull("quantity") ? 0 : row.GetValue<int>("quantity")
            })
            .Where(x => x.Quantity > 0)
            .ToList();

        if (participants.Count == 0)
        {
            _logger.LogWarning("Draw aborted for Gift {GiftId}: No purchases found.", giftId);
            throw new InvalidOperationException("לא ניתן לבצע הגרלה - אין רכישות למתנה זו");
        }

        var weightedPool = participants.SelectMany(p => Enumerable.Repeat(p, p.Quantity)).ToList();
        var winner = weightedPool[Random.Shared.Next(weightedPool.Count)];

        var drawnAt = DateTime.UtcNow;
        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.winner_by_gift
               (gift_id, gift_name, picture, winner_id, winner_name, winner_email, drawn_at)
               VALUES (?, ?, ?, ?, ?, ?, ?);",
            giftId, giftName, picture, winner.UserId, winner.UserName, winner.UserEmail, drawnAt));

        bool emailSent = false;
        try
        {
            var emailBody = $@"
                <div dir='rtl' style='width: 100%; background-color: #f9f9f9; padding: 50px 0; font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border: 2px solid #d4af37; border-radius: 10px; text-align: center;'>
                        <h1 style='color: #AEC6CF;'>מזל טוב {winner.UserName}!</h1>
                        <h2 style='font-family: Arial; color: pink'> אנו שמחים לבשר לך על זכייתך בפרס </h2>
                        <h1 style='font-family: Arial; color: Lavender'>{giftName}</h1>
                        <h3 style='font-family: Arial; color: lightgreen'>נציג יצור איתך קשר בהקדם</h3>
                        <hr>
                        <h4 style='font-family: Arial; color: lightblue'>בברכה, הנהלת המכירה הסינית</h4>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(winner.UserEmail, "מזל טוב! זכית בהגרלה", emailBody);
            emailSent = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Winning email failed for {Email}, but winner was saved in Cassandra.", winner.UserEmail);
        }

        await _session.ExecuteAsync(new SimpleStatement(
            $@"INSERT INTO {_options.Keyspace}.draw_audit_by_gift
               (gift_id, draw_time, gift_name, winner_id, winner_name, winner_email, total_tickets, email_sent)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?);",
            giftId, drawnAt, giftName, winner.UserId, winner.UserName, winner.UserEmail, totalTickets, emailSent));

        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_options.Keyspace}.gift_summary_by_gift SET winner_name = ? WHERE gift_id = ?;",
            winner.UserName, giftId));

        return new DrawingResultDTO
        {
            GiftId = giftId,
            GiftName = giftName,
            WinnerId = winner.UserId,
            WinnerName = winner.UserName,
            WinnerEmail = winner.UserEmail,
            EmailSent = emailSent,
            DrawnAtUtc = drawnAt
        };
    }

    private static string GetString(Row row, string columnName)
        => row.IsNull(columnName) ? string.Empty : row.GetValue<string>(columnName);
}
