using CatalogAPI.Metrics;
using RedisCache.Library.Interfaces;

namespace CatalogAPI.Endpoints;

public static class GamesEndpoints
{
    // In-memory store (substituir por DbContext quando implementado)
    private static readonly Dictionary<Guid, GameDto> _games = new();

    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog/games")
            .WithTags("Games");

        // ─── GET /api/catalog/games ────────────────────────────
        group.MapGet("/", async (ICacheService cacheService) =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var cacheKey = "games:all";
            var cached = await cacheService.GetAsync<List<GameDto>>(cacheKey);

            if (cached is not null)
            {
                AppMetrics.CacheHits.WithLabels("list_games").Inc();
                stopwatch.Stop();
                AppMetrics.RequestDuration.WithLabels("list_games").Observe(stopwatch.Elapsed.TotalSeconds);
                return Results.Ok(cached);
            }

            AppMetrics.CacheMisses.WithLabels("list_games").Inc();

            var games = _games.Values.ToList();
            await cacheService.SetAsync(cacheKey, games, TimeSpan.FromMinutes(5));

            stopwatch.Stop();
            AppMetrics.RequestDuration.WithLabels("list_games").Observe(stopwatch.Elapsed.TotalSeconds);
            return Results.Ok(games);
        });

        // ─── GET /api/catalog/games/{id} ───────────────────────
        group.MapGet("/{id:guid}", async (Guid id, ICacheService cacheService) =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var cacheKey = $"games:{id}";
            var cached = await cacheService.GetAsync<GameDto>(cacheKey);

            if (cached is not null)
            {
                AppMetrics.CacheHits.WithLabels("get_game").Inc();
                stopwatch.Stop();
                AppMetrics.RequestDuration.WithLabels("get_game").Observe(stopwatch.Elapsed.TotalSeconds);
                return Results.Ok(cached);
            }

            AppMetrics.CacheMisses.WithLabels("get_game").Inc();

            if (!_games.TryGetValue(id, out var game))
            {
                stopwatch.Stop();
                return Results.NotFound();
            }

            await cacheService.SetAsync(cacheKey, game, TimeSpan.FromMinutes(15));

            stopwatch.Stop();
            AppMetrics.RequestDuration.WithLabels("get_game").Observe(stopwatch.Elapsed.TotalSeconds);
            return Results.Ok(game);
        });

        // ─── POST /api/catalog/games ───────────────────────────
        group.MapPost("/", async (CreateGameRequest request, ICacheService cacheService) =>
        {
            var game = new GameDto
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                CreatedAt = DateTime.UtcNow
            };

            _games[game.Id] = game;

            // Invalidar lista
            await cacheService.RemoveAsync("games:all");

            AppMetrics.GamesCreated.Inc();
            return Results.Created($"/api/catalog/games/{game.Id}", game);
        });

        // ─── PUT /api/catalog/games/{id} ───────────────────────
        group.MapPut("/{id:guid}", async (Guid id, UpdateGameRequest request, ICacheService cacheService) =>
        {
            if (!_games.TryGetValue(id, out var game))
                return Results.NotFound();

            game.Name = request.Name ?? game.Name;
            game.Description = request.Description ?? game.Description;
            game.Price = request.Price ?? game.Price;
            game.Category = request.Category ?? game.Category;

            _games[id] = game;

            // Invalidar cache do jogo e da lista
            await cacheService.RemoveAsync($"games:{id}");
            await cacheService.RemoveAsync("games:all");

            return Results.Ok(game);
        });

        // ─── DELETE /api/catalog/games/{id} ────────────────────
        group.MapDelete("/{id:guid}", async (Guid id, ICacheService cacheService) =>
        {
            if (!_games.Remove(id))
                return Results.NotFound();

            // Invalidar cache do jogo e da lista
            await cacheService.RemoveAsync($"games:{id}");
            await cacheService.RemoveAsync("games:all");

            return Results.NoContent();
        });
    }
}

// ─── DTOs ──────────────────────────────────────────────────────
public class GameDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class UpdateGameRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
}
