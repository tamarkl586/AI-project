namespace TicketingService.Contracts;

public class TicketPurchaseInitiatedEvent
{
    public Guid PurchaseRequestId { get; set; }
    public Guid CorrelationId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int CartId { get; set; }
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public string GiftPicture { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public DateTime InitiatedAtUtc { get; set; }
}
