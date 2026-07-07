using MassTransit;
using TicketingService.Contracts;
using TicketingService.DAL.Interfaces;

namespace TicketingService.Consumers;

public class PrizeDrawingVerifiedConsumer : IConsumer<PrizeDrawingVerifiedEvent>
{
    private readonly IPurchaseRequestDAL _purchaseRequestDal;
    private readonly ICartDAL _cartDal;
    private readonly ILogger<PrizeDrawingVerifiedConsumer> _logger;

    public PrizeDrawingVerifiedConsumer(
        IPurchaseRequestDAL purchaseRequestDal,
        ICartDAL cartDal,
        ILogger<PrizeDrawingVerifiedConsumer> logger)
    {
        _purchaseRequestDal = purchaseRequestDal;
        _cartDal = cartDal;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PrizeDrawingVerifiedEvent> context)
    {
        var message = context.Message;

        // Attach the message's CorrelationId to every log statement in this consumer
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", message.CorrelationId.ToString()))
        {
            var request = await _purchaseRequestDal.GetByIdAsync(message.PurchaseRequestId);
            if (request == null)
            {
                _logger.LogWarning("Verified event received for unknown purchase request {RequestId}", message.PurchaseRequestId);
                return;
            }

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Ignoring duplicate verified event for request {RequestId}. Current status: {Status}", request.Id, request.Status);
                return;
            }

            var cartItem = await _cartDal.GetByIdAsync(request.CartId);
            if (cartItem == null)
            {
                request.Status = "Rejected";
                request.FailureReason = "Cart item was not found during verification finalization.";
                request.UpdatedAtUtc = DateTime.UtcNow;
                await _purchaseRequestDal.UpdateAsync(request);
                _logger.LogWarning("Purchase request {RequestId} rejected because cart item {CartId} is missing.", request.Id, request.CartId);
                return;
            }

            if (!cartItem.IsPurchased)
            {
                cartItem.IsPurchased = true;
                cartItem.PurchasedAt = DateTime.UtcNow;
                await _cartDal.UpdateAsync(cartItem);
            }

            request.Status = "Success";
            request.FailureReason = null;
            request.UpdatedAtUtc = DateTime.UtcNow;
            await _purchaseRequestDal.UpdateAsync(request);

            _logger.LogInformation(
                "Purchase request {RequestId} finalized successfully for gift {GiftId}.",
                request.Id,
                request.GiftId);
        }
    }
}
