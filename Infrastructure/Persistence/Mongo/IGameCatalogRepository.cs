namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public interface IGameCatalogRepository
{
    Task<List<GameDocument>> GetAllAsync();
    Task<GameDocument?> GetByIdAsync(string id);
    Task InsertAsync(GameDocument game);
    Task<bool> UpdateAsync(GameDocument game);
    Task<bool> DeleteAsync(string id);
}
