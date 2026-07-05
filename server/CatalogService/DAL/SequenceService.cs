using MongoDB.Bson;
using MongoDB.Driver;

namespace CatalogService.DAL
{
    /// <summary>
    /// Provides auto-increment integer IDs for MongoDB collections using a dedicated
    /// "counters" collection with atomic FindOneAndUpdate operations.
    /// </summary>
    public class SequenceService
    {
        private readonly IMongoCollection<BsonDocument> _counters;

        public SequenceService(CatalogDbContext context)
        {
            _counters = context.Counters;
        }

        /// <summary>
        /// Atomically increments the named counter and returns the new value.
        /// Creates the counter document if it does not exist (upsert).
        /// </summary>
        public async Task<int> GetNextIdAsync(string sequenceName)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", sequenceName);
            var update = Builders<BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = await _counters.FindOneAndUpdateAsync(filter, update, options);
            return result["seq"].AsInt32;
        }
    }
}
