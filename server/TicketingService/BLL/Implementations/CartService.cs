using AutoMapper;
using TicketingService.BLL.Interfaces;
using TicketingService.DAL.Interfaces;
using TicketingService.DTOs.Cart;
using TicketingService.Models;

namespace TicketingService.BLL.Implementations
{
    public class CartService : ICartService
    {
        private readonly ICartDAL _cartDal;
        private readonly IMapper _mapper;
        private readonly ILogger<CartService> _logger;

        public CartService(ICartDAL cartDal, IMapper mapper, ILogger<CartService> logger)
        {
            _cartDal = cartDal;
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
            var gift = await _cartDal.GetGiftByIdAsync(dto.GiftId.Value)
                ?? throw new KeyNotFoundException($"Gift ID {dto.GiftId} not found.");

            if (gift.WinnerId != null)
            {
                throw new InvalidOperationException("A draw has already been performed for this gift.");
            }

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

        public async Task PurchaseAsync(int userId)
        {
            _logger.LogInformation("Starting checkout transaction for user {UserId}", userId);

            var items = await _cartDal.GetUserCartAsync(userId);
            if (!items.Any())
            {
                throw new InvalidOperationException("Cart is empty.");
            }

            var drawnItems = items.Where(i => i.Gift.WinnerId != null).ToList();
            if (drawnItems.Any())
            {
                foreach (var drawnItem in drawnItems)
                {
                    await _cartDal.DeleteAsync(drawnItem);
                }
            }

            var hasPurchasableItems = items.Any(i => i.Gift.WinnerId == null);
            if (!hasPurchasableItems)
            {
                throw new InvalidOperationException("All items in cart were already drawn and removed.");
            }

            await _cartDal.ExecutePurchaseAsync(userId);
        }

        public async Task ClearCartAsync(int userId)
        {
            await _cartDal.ClearUserCartAsync(userId);
        }
    }
}
