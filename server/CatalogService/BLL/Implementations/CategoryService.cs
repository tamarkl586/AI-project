using AutoMapper;
using CatalogService.BLL.Interfaces;
using CatalogService.DAL.Interfaces;
using CatalogService.DTOs.Category;
using CatalogService.Models;

namespace CatalogService.BLL.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryDAL _dal;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryDAL dal, IMapper mapper, ILogger<CategoryService> logger)
        {
            _dal = dal;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<CategoryDTO>> GetAllAsync()
        {
            _logger.LogDebug("Fetching categories from database.");
            var categories = await _dal.GetAllAsync();
            return _mapper.Map<List<CategoryDTO>>(categories);
        }

        public async Task AddAsync(CategoryCreateDTO dto)
        {
            _logger.LogInformation("Processing add category: {CategoryName}", dto.Name);

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name == "string")
                throw new ArgumentException("שם הקטגוריה הוא שדה חובה");

            var existing = await _dal.GetByNameAsync(dto.Name);
            if (existing != null)
                throw new InvalidOperationException("קטגוריה בשם זה כבר קיימת במערכת");

            var category = _mapper.Map<Category>(dto);
            await _dal.AddAsync(category);
            _logger.LogInformation("Category '{CategoryName}' persisted with ID {Id}.", category.Name, category.Id);
        }

        public async Task UpdateAsync(int id, CategoryCreateDTO dto)
        {
            _logger.LogInformation("Processing update for category ID {Id}.", id);

            var category = await _dal.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("הקטגוריה לא נמצאה");

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name == "string")
                throw new ArgumentException("שם הקטגוריה הוא שדה חובה");

            var existing = await _dal.GetByNameAsync(dto.Name);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException("קיים כבר שם כזה במערכת");

            category.Name = dto.Name;
            await _dal.UpdateAsync(category);
            _logger.LogInformation("Category ID {Id} updated.", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Processing deletion for category ID {Id}.", id);

            var category = await _dal.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("הקטגוריה לא נמצאה");

            await _dal.DeleteAsync(id);
            _logger.LogInformation("Category ID {Id} removed.", id);
        }
    }
}
