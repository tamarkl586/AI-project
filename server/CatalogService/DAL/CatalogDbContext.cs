using CatalogService.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CatalogService.DAL
{
    /// <summary>
    /// MongoDB context wrapper — provides typed collection accessors for the catalog domain.
    /// Registered as Singleton; MongoClient is thread-safe.
    /// </summary>
    public class CatalogDbContext
    {
        private readonly IMongoDatabase _database;

        public CatalogDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB_ConnectionString"]
                ?? throw new InvalidOperationException("MongoDB_ConnectionString is not configured.");
            var databaseName = configuration["MongoDB_DatabaseName"] ?? "catalogDb";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Gift> Gifts => _database.GetCollection<Gift>("gifts");
        public IMongoCollection<Donor> Donors => _database.GetCollection<Donor>("donors");
        public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");

        /// <summary>Counters collection used by SequenceService for auto-increment int IDs.</summary>
        public IMongoCollection<BsonDocument> Counters => _database.GetCollection<BsonDocument>("counters");
    }
}
