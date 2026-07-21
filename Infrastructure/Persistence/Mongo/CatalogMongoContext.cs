using MongoDB.Driver;

namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public class CatalogMongoContext
{
    private readonly IMongoDatabase _database;

    public CatalogMongoContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<GameDocument> Games => _database.GetCollection<GameDocument>("games");
    public IMongoCollection<ReviewDocument> Reviews => _database.GetCollection<ReviewDocument>("reviews");
}
