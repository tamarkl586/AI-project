using CatalogService.DAL.Interfaces;
using CatalogService.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CatalogService.DAL.Implementations
{
    public class GiftDAL : IGiftDAL
    {
        private readonly IMongoCollection<Gift> _gifts;
        private readonly SequenceService _sequence;

        public GiftDAL(CatalogDbContext context, SequenceService sequence)
        {
            _gifts = context.Gifts;
            _sequence = sequence;
        }

        public async Task<List<Gift>> GetAllAsync()
            => await _gifts.Find(_ => true).ToListAsync();

        public async Task<Gift?> GetByIdAsync(int id)
            => await _gifts.Find(g => g.Id == id).FirstOrDefaultAsync();

        public async Task<bool> ExistsByNameAsync(string name, int? currentId = null)
        {
            var filter = Builders<Gift>.Filter.Eq(g => g.Name, name);
            if (currentId.HasValue)
                filter &= Builders<Gift>.Filter.Ne(g => g.Id, currentId.Value);
            return await _gifts.Find(filter).AnyAsync();
        }

        public async Task AddAsync(Gift gift)
        {
            gift.Id = await _sequence.GetNextIdAsync("gifts");
            await _gifts.InsertOneAsync(gift);
        }

        public async Task UpdateAsync(Gift gift)
            => await _gifts.ReplaceOneAsync(g => g.Id == gift.Id, gift);

        public async Task DeleteAsync(int id)
        {
            var gift = await GetByIdAsync(id);
            if (gift == null)
                throw new KeyNotFoundException("מתנה לא נמצאה");
            if (gift.WinnerId != null)
                throw new InvalidOperationException("לא ניתן למחוק מתנה שכבר הוגרלה");

            await _gifts.DeleteOneAsync(g => g.Id == id);
        }

        public async Task<List<Gift>> SearchAsync(string? giftName, string? donorName)
        {
            var filters = new List<FilterDefinition<Gift>>();

            if (!string.IsNullOrWhiteSpace(giftName))
                filters.Add(Builders<Gift>.Filter.Regex(g => g.Name,
                    new BsonRegularExpression(giftName, "i")));

            if (!string.IsNullOrWhiteSpace(donorName))
                filters.Add(Builders<Gift>.Filter.Regex(g => g.DonorName,
                    new BsonRegularExpression(donorName, "i")));

            var combined = filters.Any()
                ? Builders<Gift>.Filter.And(filters)
                : Builders<Gift>.Filter.Empty;

            return await _gifts.Find(combined).ToListAsync();
        }

        public async Task SetWinnerAsync(int giftId, int winnerId)
        {
            var update = Builders<Gift>.Update.Set(g => g.WinnerId, winnerId);
            var result = await _gifts.UpdateOneAsync(g => g.Id == giftId, update);
            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Gift {giftId} not found");
        }

        public async Task<List<Gift>> UserSearchAsync(string? categoryName, int? maxPrice)
        {
            var filters = new List<FilterDefinition<Gift>>();

            if (!string.IsNullOrWhiteSpace(categoryName))
                filters.Add(Builders<Gift>.Filter.Regex(g => g.CategoryName,
                    new BsonRegularExpression(categoryName, "i")));

            if (maxPrice.HasValue)
                filters.Add(Builders<Gift>.Filter.Lte(g => g.Price, maxPrice.Value));

            var combined = filters.Any()
                ? Builders<Gift>.Filter.And(filters)
                : Builders<Gift>.Filter.Empty;

            return await _gifts.Find(combined).ToListAsync();
        }

        public async Task<List<Gift>> GetByDonorIdAsync(int donorId)
            => await _gifts.Find(g => g.DonorId == donorId).ToListAsync();
    }
}
