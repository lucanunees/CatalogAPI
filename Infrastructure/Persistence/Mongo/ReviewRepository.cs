using MongoDB.Driver;

namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public class ReviewRepository : IReviewRepository
{
    private readonly IMongoCollection<ReviewDocument> _collection;

    public ReviewRepository(CatalogMongoContext context)
    {
        _collection = context.Reviews;
    }

    public Task InsertAsync(ReviewDocument review) =>
        _collection.InsertOneAsync(review);

    public async Task<List<ReviewDocument>> GetByGameIdAsync(string gameId) =>
        await _collection.Find(x => x.GameId == gameId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
}
