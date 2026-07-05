using CatalogService.Models;

namespace CatalogService.DAL.Interfaces
{
    public interface IDonorDAL
    {
        Task<List<Donor>> GetAllAsync();
        Task<Donor?> GetByIdAsync(int id);
        Task<bool> ExistsByIdentityNumberAsync(string identityNumber, int? id = null);
        Task<bool> ExistsByNameAsync(string name, int? id = null);
        Task<bool> ExistsByEmailAsync(string email, int? id = null);
        Task AddAsync(Donor donor);
        Task UpdateAsync(Donor donor);
        Task DeleteAsync(int id);

        /// <param name="filterByIds">When provided, only donors whose Id is in this set are returned.</param>
        Task<List<Donor>> SearchAsync(string? donorName, string? email, IEnumerable<int>? filterByIds = null);
    }
}
