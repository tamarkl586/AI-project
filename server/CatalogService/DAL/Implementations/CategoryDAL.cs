using CatalogService.DAL.Interfaces;
using CatalogService.Models;
using MongoDB.Driver;

namespace CatalogService.DAL.Implementations
{
    public class CategoryDAL : ICategoryDAL
    {
        private readonly IMongoCollection<Category> _categories;
        private readonly SequenceService _sequence;

        public CategoryDAL(CatalogDbContext context, SequenceService sequence)
        {
            _categories = context.Categories;
            _sequence = sequence;
        }

        public async Task<List<Category>> GetAllAsync()
            => await _categories.Find(_ => true).SortBy(c => c.Id).ToListAsync();

        public async Task<Category?> GetByIdAsync(int id)
            => await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();

        public async Task<Category?> GetByNameAsync(string name)
            => await _categories.Find(c => c.Name == name).FirstOrDefaultAsync();

        public async Task AddAsync(Category category)
        {
            category.Id = await _sequence.GetNextIdAsync("categories");
            await _categories.InsertOneAsync(category);
        }

        public async Task UpdateAsync(Category category)
            => await _categories.ReplaceOneAsync(c => c.Id == category.Id, category);

        public async Task DeleteAsync(int id)
            => await _categories.DeleteOneAsync(c => c.Id == id);
    }
}
