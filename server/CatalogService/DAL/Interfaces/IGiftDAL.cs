using CatalogService.Models;

namespace CatalogService.DAL.Interfaces
{
    public interface IGiftDAL
    {
        Task<List<Gift>> GetAllAsync();
        Task<Gift?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? currentId = null);
        Task AddAsync(Gift gift);
        Task UpdateAsync(Gift gift);
        Task DeleteAsync(int id);
        Task<List<Gift>> SearchAsync(string? giftName, string? donorName);
        Task<List<Gift>> UserSearchAsync(string? categoryName, int? maxPrice);
        Task<List<Gift>> GetByDonorIdAsync(int donorId);
    }
}
