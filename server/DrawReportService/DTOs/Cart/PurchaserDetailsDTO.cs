namespace DrawReportService.DTOs.Cart;

public class PurchaserDetailsDTO
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TotalTicketsPurchased { get; set; }
    public int GrandTotalSpent { get; set; }
    public List<PurchaserItemDTO> PurchaseHistory { get; set; } = new();
}
