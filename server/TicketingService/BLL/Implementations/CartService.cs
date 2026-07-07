using AutoMapper;
using MassTransit;
using TicketingService.BLL.Interfaces;
using TicketingService.Clients.Catalog;
using TicketingService.Contracts;
using TicketingService.DAL.Interfaces;
using TicketingService.DTOs.Cart;
using TicketingService.Models;

namespace TicketingService.BLL.Implementations
{
    public class CartService : ICartService
    {
        private readonly ICartDAL _cartDal;
        private readonly ICatalogServiceClient _catalogServiceClient;
        private readonly IPurchaseRequestDAL _purchaseRequestDal;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMapper _mapper;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartDAL cartDal,
            ICatalogServiceClient catalogServiceClient,
            IPurchaseRequestDAL purchaseRequestDal,
            IPublishEndpoint publishEndpoint,
            IMapper mapper,
            ILogger<CartService> logger)
        {
            _cartDal = cartDal;
            _catalogServiceClient = catalogServiceClient;
            _purchaseRequestDal = purchaseRequestDal;
            _publishEndpoint = publishEndpoint;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<CartItemDTO>> GetMyCartAsync(int userId)
        {
            _logger.LogDebug("Loading open cart items for user {UserId}", userId);
            var items = await _cartDal.GetUserCartAsync(userId);
            return _mapper.Map<List<CartItemDTO>>(items);
        }

        public async Task AddAsync(int userId, AddToCartDTO dto)
        {
            if (dto.GiftId is null)
            {
                throw new InvalidOperationException("GiftId is required.");
            }

            _logger.LogInformation("Checking gift {GiftId} before add-to-cart for user {UserId}", dto.GiftId, userId);
            var gift = await _catalogServiceClient.GetGiftByIdAsync(dto.GiftId.Value)
                ?? throw new KeyNotFoundException($"Gift ID {dto.GiftId} not found.");

            if (gift.WinnerId != null)
            {
                throw new InvalidOperationException("A draw has already been performed for this gift.");
            }

            await _cartDal.UpsertGiftAsync(new Gift
            {
                Id = gift.Id,
                Name = gift.Name,
                Description = gift.Description,
                Picture = gift.Picture,
                Price = gift.Price,
                WinnerId = gift.WinnerId
            });

            var existingItem = await _cartDal.GetOpenCartItemAsync(userId, dto.GiftId.Value);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                await _cartDal.UpdateAsync(existingItem);
                return;
            }

            var cartItem = _mapper.Map<Cart>(dto);
            cartItem.UserID = userId;
            cartItem.CreatedAt = DateTime.UtcNow;
            await _cartDal.AddAsync(cartItem);
        }

        public async Task UpdateQuantityAsync(int cartId, int userId, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }

            var item = await _cartDal.GetByIdAsync(cartId)
                ?? throw new KeyNotFoundException("Cart item not found.");

            if (item.UserID != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this cart item.");
            }

            item.Quantity = newQuantity;
            await _cartDal.UpdateAsync(item);
        }

        public async Task RemoveAsync(int cartId, int userId)
        {
            var item = await _cartDal.GetByIdAsync(cartId)
                ?? throw new KeyNotFoundException("Cart item not found in cart.");

            if (item.UserID != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to perform this action.");
            }

            if (item.IsPurchased)
            {
                throw new InvalidOperationException("Cannot remove an item that has already been purchased.");
            }

            await _cartDal.DeleteAsync(item);
        }

        public async Task<int> PurchaseAsync(int userId, string userName, string userEmail)
        {
            _logger.LogInformation("Starting asynchronous checkout flow for user {UserId}", userId);

            var items = await _cartDal.GetUserCartAsync(userId);
            if (!items.Any())
            {
                throw new InvalidOperationException("Cart is empty.");
            }

            var correlationId = Guid.NewGuid();
            var purchaseRequests = items.Select(item => new TicketPurchaseRequest
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                CartId = item.Id,
                UserId = userId,
                GiftId = item.GiftID,
                Quantity = item.Quantity,
                UnitPrice = item.Gift.Price,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            }).ToList();

            await _purchaseRequestDal.AddRangeAsync(purchaseRequests);

            foreach (var request in purchaseRequests)
            {
                var cartItem = items.First(i => i.Id == request.CartId);

                await _publishEndpoint.Publish(new TicketPurchaseInitiatedEvent
                {
                    PurchaseRequestId = request.Id,
                    CorrelationId = request.CorrelationId,
                    UserId = request.UserId,
                    UserName = userName,
                    UserEmail = userEmail,
                    CartId = request.CartId,
                    GiftId = request.GiftId,
                    GiftName = cartItem.Gift.Name,
                    GiftPicture = cartItem.Gift.Picture,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    InitiatedAtUtc = DateTime.UtcNow
                });
            }

            _logger.LogInformation(
                "Published {Count} TicketPurchaseInitiatedEvent messages for user {UserId}. Correlation {CorrelationId}",
                purchaseRequests.Count,
                userId,
                correlationId);

            return purchaseRequests.Count;
        }

        public async Task ClearCartAsync(int userId)
        {
            await _cartDal.ClearUserCartAsync(userId);
        }
    }
}
