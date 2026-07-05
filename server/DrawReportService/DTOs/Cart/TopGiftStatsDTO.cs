namespace DrawReportService.DTOs.Cart;

public class TopGiftStatsDTO
{
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public int Price { get; set; }
    public int TotalTicketsPurchased { get; set; }
    public decimal TotalEarned { get; set; }
}
