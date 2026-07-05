namespace DrawReportService.DTOs.Cart;

public class GiftPurchaseDTO
{
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
