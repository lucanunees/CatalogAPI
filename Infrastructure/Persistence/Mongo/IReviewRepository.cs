namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public interface IReviewRepository
{
    Task InsertAsync(ReviewDocument review);
    Task<List<ReviewDocument>> GetByGameIdAsync(string gameId);
}
