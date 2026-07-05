using TicketingService.DTOs.Cart;

namespace TicketingService.BLL.Interfaces
{
    public interface ICartService
    {
        Task<List<CartItemDTO>> GetMyCartAsync(int userId);
        Task AddAsync(int userId, AddToCartDTO dto);
        Task UpdateQuantityAsync(int cartId, int userId, int newQuantity);
        Task RemoveAsync(int cartId, int userId);
        Task PurchaseAsync(int userId);
        Task ClearCartAsync(int userId);
    }
}

