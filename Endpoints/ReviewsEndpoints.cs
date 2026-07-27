using CatalogAPI.Infrastructure.Persistence.Mongo;
using RedisCache.Library.Interfaces;

namespace CatalogAPI.Endpoints;

public static class ReviewsEndpoints
{
    public static void MapReviewsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog/games/{gameId}/reviews")
            .WithTags("Reviews");

        // ─── GET /api/catalog/games/{gameId}/reviews ───────────
        group.MapGet("/", async (string gameId, IReviewRepository reviewRepository, ICacheService cacheService) =>
        {
            var cacheKey = $"reviews:{gameId}";
            var cached = await cacheService.GetAsync<List<ReviewDto>>(cacheKey);
            if (cached is not null) return Results.Ok(cached);

            var reviews = await reviewRepository.GetByGameIdAsync(gameId);
            var dtos = reviews.Select(ToDto).ToList();
            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));

            return Results.Ok(dtos);
        });

        // ─── POST /api/catalog/games/{gameId}/reviews ──────────
        group.MapPost("/", async (
            string gameId,
            CreateReviewRequest request,
            IGameCatalogRepository gameRepository,
            IReviewRepository reviewRepository,
            ICacheService cacheService) =>
        {
            if (request.Rating is < 1 or > 5)
                return Results.BadRequest(new { message = "Rating must be between 1 and 5" });

            var game = await gameRepository.GetByIdAsync(gameId);
            if (game is null) return Results.NotFound(new { message = "Game not found" });

            var review = new ReviewDocument
            {
                Id = Guid.NewGuid().ToString(),
                GameId = gameId,
                UserId = request.UserId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };
            await reviewRepository.InsertAsync(review);

            var allReviews = await reviewRepository.GetByGameIdAsync(gameId);
            game.ReviewCount = allReviews.Count;
            game.AverageRating = allReviews.Average(r => r.Rating);
            game.UpdatedAt = DateTime.UtcNow;
            await gameRepository.UpdateAsync(game);

            await cacheService.RemoveAsync($"reviews:{gameId}");
            await cacheService.RemoveAsync($"games:{gameId}");
            await cacheService.RemoveAsync("games:all");

            return Results.Created($"/api/catalog/games/{gameId}/reviews/{review.Id}", ToDto(review));
        });
    }

    private static ReviewDto ToDto(ReviewDocument review) => new()
    {
        Id = review.Id,
        GameId = review.GameId,
        UserId = review.UserId,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt
    };
}

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
