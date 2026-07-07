using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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

        public async Task UpsertGiftAsync(Gift gift)
        {
            var existingGift = await _context.Gifts.FirstOrDefaultAsync(g => g.Id == gift.Id);
            if (existingGift == null)
            {
                await InsertGiftSnapshotAsync(gift);
            }
            else
            {
                existingGift.Name = gift.Name;
                existingGift.Description = gift.Description;
                existingGift.Picture = gift.Picture;
                existingGift.Price = gift.Price;
                existingGift.WinnerId = gift.WinnerId;
            }

            await _context.SaveChangesAsync();
        }

        private async Task InsertGiftSnapshotAsync(Gift gift)
        {
            await _context.Gifts.AddAsync(gift);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsExplicitIdentityInsertError(ex))
            {
                _context.Entry(gift).State = EntityState.Detached;

                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Gifts] ON;");
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO [Gifts] ([Id], [Description], [Name], [Picture], [Price], [WinnerId])
                    VALUES ({gift.Id}, {gift.Description}, {gift.Name}, {gift.Picture}, {gift.Price}, {gift.WinnerId})");
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Gifts] OFF;");
                await transaction.CommitAsync();
            }
        }

        private static bool IsExplicitIdentityInsertError(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlException
                && sqlException.Number == 544;
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
