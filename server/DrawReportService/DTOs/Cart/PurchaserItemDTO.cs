namespace DrawReportService.DTOs.Cart;

public class PurchaserItemDTO
{
    public string GiftName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PricePerUnit { get; set; }
    public int TotalPrice { get; set; }
    public DateTime PurchaseDate { get; set; }
}
