namespace DrawReportService.DTOs.Cart;

public class GiftPurchasesSummaryDTO
{
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public List<GiftPurchaseDTO> Purchasers { get; set; } = new();
    public int TotalTicketsPurchased { get; set; }
    public decimal TotalEarned { get; set; }
}
