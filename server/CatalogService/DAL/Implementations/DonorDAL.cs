using CatalogService.DAL.Interfaces;
using CatalogService.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CatalogService.DAL.Implementations
{
    public class DonorDAL : IDonorDAL
    {
        private readonly IMongoCollection<Donor> _donors;
        private readonly SequenceService _sequence;

        public DonorDAL(CatalogDbContext context, SequenceService sequence)
        {
            _donors = context.Donors;
            _sequence = sequence;
        }

        public async Task<List<Donor>> GetAllAsync()
            => await _donors.Find(_ => true).ToListAsync();

        public async Task<Donor?> GetByIdAsync(int id)
            => await _donors.Find(d => d.Id == id).FirstOrDefaultAsync();

        public async Task<bool> ExistsByIdentityNumberAsync(string identityNumber, int? id = null)
        {
            var filter = Builders<Donor>.Filter.Eq(d => d.IdentityNumber, identityNumber);
            if (id.HasValue)
                filter &= Builders<Donor>.Filter.Ne(d => d.Id, id.Value);
            return await _donors.Find(filter).AnyAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? id = null)
        {
            var filter = Builders<Donor>.Filter.Eq(d => d.Name, name);
            if (id.HasValue)
                filter &= Builders<Donor>.Filter.Ne(d => d.Id, id.Value);
            return await _donors.Find(filter).AnyAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? id = null)
        {
            var filter = Builders<Donor>.Filter.Eq(d => d.Email, email);
            if (id.HasValue)
                filter &= Builders<Donor>.Filter.Ne(d => d.Id, id.Value);
            return await _donors.Find(filter).AnyAsync();
        }

        public async Task AddAsync(Donor donor)
        {
            donor.Id = await _sequence.GetNextIdAsync("donors");
            await _donors.InsertOneAsync(donor);
        }

        public async Task UpdateAsync(Donor donor)
            => await _donors.ReplaceOneAsync(d => d.Id == donor.Id, donor);

        public async Task DeleteAsync(int id)
            => await _donors.DeleteOneAsync(d => d.Id == id);

        public async Task<List<Donor>> SearchAsync(string? donorName, string? email,
            IEnumerable<int>? filterByIds = null)
        {
            var filters = new List<FilterDefinition<Donor>>();

            if (!string.IsNullOrWhiteSpace(donorName))
                filters.Add(Builders<Donor>.Filter.Regex(d => d.Name,
                    new BsonRegularExpression(donorName, "i")));

            if (!string.IsNullOrWhiteSpace(email))
                filters.Add(Builders<Donor>.Filter.Regex(d => d.Email,
                    new BsonRegularExpression(email, "i")));

            if (filterByIds != null)
                filters.Add(Builders<Donor>.Filter.In(d => d.Id, filterByIds));

            var combined = filters.Any()
                ? Builders<Donor>.Filter.And(filters)
                : Builders<Donor>.Filter.Empty;

            return await _donors.Find(combined).ToListAsync();
        }
    }
}
