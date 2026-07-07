using System.ComponentModel.DataAnnotations;

namespace TicketingService.Models;

public class TicketPurchaseRequest
{
    [Key]
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }

    public int CartId { get; set; }

    public int UserId { get; set; }

    public int GiftId { get; set; }

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Pending";

    [MaxLength(256)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
