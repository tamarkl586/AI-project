namespace TicketingService.Contracts;

public class PrizeDrawingClosedEvent
{
    public Guid PurchaseRequestId { get; set; }
    public Guid CorrelationId { get; set; }
    public int GiftId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RejectedAtUtc { get; set; }
}
