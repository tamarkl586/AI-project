using CatalogService.DTOs.Gift;

namespace CatalogService.BLL.Interfaces
{
    public interface IGiftService
    {
        Task<List<GiftDTO>> GetAllAsync();
        Task<GiftDTO?> GetByIdAsync(int id);
        Task AddAsync(CreateGiftDTO dto);
        Task UpdateAsync(int id, GiftUpdateDTO dto);
        Task DeleteAsync(int id);

        /// <summary>Manager search — filters by gift name and/or donor name. No cart/buyer data.</summary>
        Task<List<GiftDTO>> ManagerSearchAsync(string? giftName, string? donorName);

        Task<List<GiftDTO>> UserSearchAsync(string? categoryName, int? maxPrice);
    }
}
