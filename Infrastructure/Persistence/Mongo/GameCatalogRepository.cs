using MongoDB.Driver;

namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public class GameCatalogRepository : IGameCatalogRepository
{
    private readonly IMongoCollection<GameDocument> _collection;

    public GameCatalogRepository(CatalogMongoContext context)
    {
        _collection = context.Games;
    }

    public async Task<List<GameDocument>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<GameDocument?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public Task InsertAsync(GameDocument game) =>
        _collection.InsertOneAsync(game);

    public async Task<bool> UpdateAsync(GameDocument game)
    {
        var result = await _collection.ReplaceOneAsync(x => x.Id == game.Id, game);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0;
    }
}
