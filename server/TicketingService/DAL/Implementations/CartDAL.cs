using Microsoft.EntityFrameworkCore;
using TicketingService.DAL.Interfaces;
using TicketingService.Models;

namespace TicketingService.DAL.Implementations
{
    public class CartDAL : ICartDAL
    {
        private readonly TicketingDbContext _context;

        public CartDAL(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cart>> GetUserCartAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.Gift)
                .Where(c => c.UserID == userId && !c.IsPurchased)
                .ToListAsync();
        }

        public async Task<Cart?> GetByIdAsync(int id)
        {
            return await _context.Carts
                .Include(c => c.Gift)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cart?> GetOpenCartItemAsync(int userId, int giftId)
        {
            return await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId && c.GiftID == giftId && !c.IsPurchased);
        }

        public async Task<Gift?> GetGiftByIdAsync(int giftId)
        {
            return await _context.Gifts.FirstOrDefaultAsync(g => g.Id == giftId);
        }

        public async Task AddAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Cart cart)
        {
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Cart cart)
        {
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }

        public async Task ExecutePurchaseAsync(int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var items = await _context.Carts
                .Where(c => c.UserID == userId && !c.IsPurchased)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsPurchased = true;
                item.PurchasedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ClearUserCartAsync(int userId)
        {
            var items = await _context.Carts
                .Where(c => c.UserID == userId && !c.IsPurchased)
                .ToListAsync();

            _context.Carts.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
