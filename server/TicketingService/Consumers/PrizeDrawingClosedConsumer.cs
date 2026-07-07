using MassTransit;
using TicketingService.Contracts;
using TicketingService.DAL.Interfaces;

namespace TicketingService.Consumers;

public class PrizeDrawingClosedConsumer : IConsumer<PrizeDrawingClosedEvent>
{
    private readonly IPurchaseRequestDAL _purchaseRequestDal;
    private readonly ICartDAL _cartDal;
    private readonly ILogger<PrizeDrawingClosedConsumer> _logger;

    public PrizeDrawingClosedConsumer(
        IPurchaseRequestDAL purchaseRequestDal,
        ICartDAL cartDal,
        ILogger<PrizeDrawingClosedConsumer> logger)
    {
        _purchaseRequestDal = purchaseRequestDal;
        _cartDal = cartDal;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PrizeDrawingClosedEvent> context)
    {
        var message = context.Message;

        // Attach the message's CorrelationId to every log statement in this consumer
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", message.CorrelationId.ToString()))
        {
            var request = await _purchaseRequestDal.GetByIdAsync(message.PurchaseRequestId);
            if (request == null)
            {
                _logger.LogWarning("Closed event received for unknown purchase request {RequestId}", message.PurchaseRequestId);
                return;
            }

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Ignoring duplicate closed event for request {RequestId}. Current status: {Status}", request.Id, request.Status);
                return;
            }

            var cartItem = await _cartDal.GetByIdAsync(request.CartId);
            if (cartItem != null && !cartItem.IsPurchased)
            {
                await _cartDal.DeleteAsync(cartItem);
            }

            request.Status = "Rejected";
            request.FailureReason = message.Reason;
            request.UpdatedAtUtc = DateTime.UtcNow;
            await _purchaseRequestDal.UpdateAsync(request);

            _logger.LogWarning(
                "Compensation completed for purchase request {RequestId}. Gift {GiftId} rejected: {Reason}.",
                request.Id,
                request.GiftId,
                message.Reason);
        }
    }
}
