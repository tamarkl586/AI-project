using TicketingService.Models;

namespace TicketingService.DAL.Interfaces
{
    public interface ICartDAL
    {
        Task<List<Cart>> GetUserCartAsync(int userId);
        Task<Cart?> GetByIdAsync(int id);
        Task<Cart?> GetOpenCartItemAsync(int userId, int giftId);
        Task UpsertGiftAsync(Gift gift);

        Task AddAsync(Cart cart);
        Task UpdateAsync(Cart cart);
        Task DeleteAsync(Cart cart);

        Task ExecutePurchaseAsync(int userId);
        Task ClearUserCartAsync(int userId);
    }
}
