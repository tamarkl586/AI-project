using CatalogService.DTOs.Category;

namespace CatalogService.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDTO>> GetAllAsync();
        Task AddAsync(CategoryCreateDTO dto);
        Task UpdateAsync(int id, CategoryCreateDTO dto);
        Task DeleteAsync(int id);
    }
}
