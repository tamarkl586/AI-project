namespace TicketingService.Contracts;

public class PrizeDrawingVerifiedEvent
{
    public Guid PurchaseRequestId { get; set; }
    public Guid CorrelationId { get; set; }
    public int GiftId { get; set; }
    public DateTime VerifiedAtUtc { get; set; }
}
