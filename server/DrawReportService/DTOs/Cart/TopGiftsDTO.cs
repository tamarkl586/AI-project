namespace DrawReportService.DTOs.Cart;

public class TopGiftsDTO
{
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string WinnerName { get; set; } = "N/A";
    public int TotalTicketsPurchased { get; set; }
    public decimal TotalEarned { get; set; }
    public int Price { get; set; }
}
