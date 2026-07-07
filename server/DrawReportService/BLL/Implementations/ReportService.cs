using Cassandra;
using DrawReportService.DAL;
using DrawReportService.BLL.Interfaces;
using DrawReportService.DTOs.Cart;
using DrawReportService.DTOs.Report;
using DrawReportService.Options;
using Microsoft.Extensions.Options;

namespace DrawReportService.BLL.Implementations;

public class ReportService : IReportService
{
    private readonly Cassandra.ISession _session;
    private readonly CassandraOptions _options;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ICassandraSessionFactory sessionFactory, IOptions<CassandraOptions> options, ILogger<ReportService> logger)
    {
        _session = sessionFactory.Session;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<GiftWinnerReportDTO>> GetWinnersReportAsync()
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement($"SELECT gift_name, picture, winner_name, winner_email FROM {_options.Keyspace}.winner_by_gift;"));

        var report = rows.Select(row => new GiftWinnerReportDTO
            {
                GiftName = GetString(row, "gift_name"),
                Picture = GetString(row, "picture"),
                WinnerName = GetString(row, "winner_name"),
                ContactEmail = GetString(row, "winner_email")
            })
            .ToList();

        _logger.LogInformation("Successfully loaded {Count} winner rows.", report.Count);
        return report;
    }

    public async Task<RevenueSummaryDTO> GetRevenueSummaryAsync()
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement($"SELECT total_earned, total_tickets FROM {_options.Keyspace}.gift_summary_by_gift;"));

        var totalRevenue = 0m;
        var totalTicketsSold = 0;

        foreach (var row in rows)
        {
            totalRevenue += row.IsNull("total_earned") ? 0m : row.GetValue<decimal>("total_earned");
            totalTicketsSold += row.IsNull("total_tickets") ? 0 : row.GetValue<int>("total_tickets");
        }

        var participantsRows = await _session.ExecuteAsync(new SimpleStatement($"SELECT user_id FROM {_options.Keyspace}.purchaser_analytics_by_user;"));
        var totalParticipants = participantsRows.Count();

        return new RevenueSummaryDTO
        {
            TotalRevenue = totalRevenue,
            TotalTicketsSold = totalTicketsSold,
            TotalParticipants = totalParticipants
        };
    }

    public async Task<List<PurchaserDetailsDTO>> GetAllPurchasersAsync()
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement($"SELECT user_id, name, email, phone, total_tickets, grand_total_spent FROM {_options.Keyspace}.purchaser_analytics_by_user;"));

        var result = new List<PurchaserDetailsDTO>();
        foreach (var row in rows)
        {
            var userId = row.GetValue<int>("user_id");
            var details = new PurchaserDetailsDTO
            {
                UserId = userId,
                Name = GetString(row, "name"),
                Email = GetString(row, "email"),
                Phone = GetString(row, "phone"),
                TotalTicketsPurchased = row.IsNull("total_tickets") ? 0 : row.GetValue<int>("total_tickets"),
                GrandTotalSpent = row.IsNull("grand_total_spent") ? 0 : row.GetValue<int>("grand_total_spent"),
                PurchaseHistory = await GetPurchaserItemsAsync(userId)
            };

            result.Add(details);
        }

        return result;
    }

    public async Task<PurchaserDetailsDTO> GetPurchaserDetailsAsync(int userId)
    {
        var statement = new SimpleStatement(
            $"SELECT user_id, name, email, phone, total_tickets, grand_total_spent FROM {_options.Keyspace}.purchaser_analytics_by_user WHERE user_id = ?;",
            userId);

        var row = (await _session.ExecuteAsync(statement)).FirstOrDefault();
        if (row == null)
        {
            throw new KeyNotFoundException("No purchases were found for this user.");
        }

        return new PurchaserDetailsDTO
        {
            UserId = userId,
            Name = GetString(row, "name"),
            Email = GetString(row, "email"),
            Phone = GetString(row, "phone"),
            TotalTicketsPurchased = row.IsNull("total_tickets") ? 0 : row.GetValue<int>("total_tickets"),
            GrandTotalSpent = row.IsNull("grand_total_spent") ? 0 : row.GetValue<int>("grand_total_spent"),
            PurchaseHistory = await GetPurchaserItemsAsync(userId)
        };
    }

    public async Task<GiftPurchasesSummaryDTO> GetPurchasesByGiftIdAsync(int giftId)
    {
        var summaryStatement = new SimpleStatement(
            $"SELECT gift_name, total_tickets, total_earned FROM {_options.Keyspace}.gift_summary_by_gift WHERE gift_id = ?;",
            giftId);

        var summaryRow = (await _session.ExecuteAsync(summaryStatement)).FirstOrDefault();

        var purchasesStatement = new SimpleStatement(
            $"SELECT user_name, user_email, quantity FROM {_options.Keyspace}.gift_purchases_by_gift WHERE gift_id = ?;",
            giftId);

        var purchaseRows = await _session.ExecuteAsync(purchasesStatement);
        var purchasers = purchaseRows.Select(row => new GiftPurchaseDTO
            {
                BuyerName = GetString(row, "user_name"),
                BuyerEmail = GetString(row, "user_email"),
                Quantity = row.IsNull("quantity") ? 0 : row.GetValue<int>("quantity")
            })
            .ToList();

        if (summaryRow == null && purchasers.Count == 0)
        {
            throw new KeyNotFoundException("Gift not found in the system.");
        }

        var calculatedTickets = purchasers.Sum(p => p.Quantity);
        var calculatedEarned = calculatedTickets * 0m;

        return new GiftPurchasesSummaryDTO
        {
            GiftId = giftId,
            GiftName = summaryRow == null ? $"Gift {giftId}" : GetString(summaryRow, "gift_name"),
            Purchasers = purchasers,
            TotalTicketsPurchased = summaryRow == null
                ? calculatedTickets
                : (summaryRow.IsNull("total_tickets") ? 0 : summaryRow.GetValue<int>("total_tickets")),
            TotalEarned = summaryRow == null
                ? calculatedEarned
                : (summaryRow.IsNull("total_earned") ? 0m : summaryRow.GetValue<decimal>("total_earned"))
        };
    }

    public async Task<TopGiftsDTO?> FindTopGiftAsync(string criteria)
    {
        if (string.IsNullOrWhiteSpace(criteria))
        {
            throw new ArgumentException("A valid search type must be provided.", nameof(criteria));
        }

        var normalized = criteria.Trim().ToLowerInvariant();
        if (normalized != "tickets" && normalized != "revenue")
        {
            throw new ArgumentException("Invalid search value. Only 'tickets' or 'revenue' are allowed.", nameof(criteria));
        }

        var rows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT gift_id, gift_name, picture, price, total_tickets, total_earned, winner_name FROM {_options.Keyspace}.gift_summary_by_gift;"));

        var stats = rows.Select(row => new TopGiftStatsDTO
        {
            GiftId = row.GetValue<int>("gift_id"),
            GiftName = GetString(row, "gift_name"),
            Picture = GetString(row, "picture"),
            Price = row.IsNull("price") ? 0 : row.GetValue<int>("price"),
            TotalTicketsPurchased = row.IsNull("total_tickets") ? 0 : row.GetValue<int>("total_tickets"),
            TotalEarned = row.IsNull("total_earned") ? 0m : row.GetValue<decimal>("total_earned")
        }).ToList();

        if (stats.Count == 0)
        {
            return null;
        }

        var top = normalized == "tickets"
            ? stats.OrderByDescending(x => x.TotalTicketsPurchased).ThenByDescending(x => x.TotalEarned).First()
            : stats.OrderByDescending(x => x.TotalEarned).ThenByDescending(x => x.TotalTicketsPurchased).First();

        var winnerName = rows.FirstOrDefault(r => r.GetValue<int>("gift_id") == top.GiftId)?.GetValue<string>("winner_name") ?? "N/A";

        return new TopGiftsDTO
        {
            GiftId = top.GiftId,
            GiftName = top.GiftName,
            Picture = top.Picture,
            Price = top.Price,
            TotalTicketsPurchased = top.TotalTicketsPurchased,
            TotalEarned = top.TotalEarned,
            WinnerName = string.IsNullOrWhiteSpace(winnerName) ? "N/A" : winnerName
        };
    }

    private async Task<List<PurchaserItemDTO>> GetPurchaserItemsAsync(int userId)
    {
        var statement = new SimpleStatement(
            $"SELECT gift_name, quantity, price_per_unit, total_price, purchased_at FROM {_options.Keyspace}.purchaser_items_by_user WHERE user_id = ?;",
            userId);

        var rows = await _session.ExecuteAsync(statement);
        return rows.Select(row => new PurchaserItemDTO
            {
                GiftName = GetString(row, "gift_name"),
                Quantity = row.IsNull("quantity") ? 0 : row.GetValue<int>("quantity"),
                PricePerUnit = row.IsNull("price_per_unit") ? 0 : row.GetValue<int>("price_per_unit"),
                TotalPrice = row.IsNull("total_price") ? 0 : row.GetValue<int>("total_price"),
                PurchaseDate = row.IsNull("purchased_at") ? DateTime.MinValue : row.GetValue<DateTime>("purchased_at")
            })
            .ToList();
    }

    private static string GetString(Row row, string columnName)
        => row.IsNull(columnName) ? string.Empty : row.GetValue<string>(columnName);
}
