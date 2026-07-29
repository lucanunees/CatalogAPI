using CatalogAPI.Infrastructure.Persistence.Mongo;
using CatalogAPI.Metrics;
using RedisCache.Library.Interfaces;
using CatalogAPI.Infrastructure.Persistence;
using CatalogAPI.Domain.Elastic;
using Elastic.Clients.Elasticsearch;

namespace CatalogAPI.Endpoints;

public static class GamesEndpoints
{
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog/games")
            .WithTags("Games");

        // ─── GET /api/catalog/games ────────────────────────────
        group.MapGet("/", async (IGameCatalogRepository repository, ICacheService cacheService) =>
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

            var documents = await repository.GetAllAsync();
            var games = documents.Select(ToDto).ToList();
            await cacheService.SetAsync(cacheKey, games, TimeSpan.FromMinutes(5));

            stopwatch.Stop();
            AppMetrics.RequestDuration.WithLabels("list_games").Observe(stopwatch.Elapsed.TotalSeconds);
            return Results.Ok(games);
        });

        // ─── GET /api/catalog/games/{id} ───────────────────────
        group.MapGet("/{id}", async (string id, IGameCatalogRepository repository, ICacheService cacheService) =>
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

            var document = await repository.GetByIdAsync(id);
            if (document is null)
            {
                stopwatch.Stop();
                return Results.NotFound();
            }

            var game = ToDto(document);
            await cacheService.SetAsync(cacheKey, game, TimeSpan.FromMinutes(15));

            stopwatch.Stop();
            AppMetrics.RequestDuration.WithLabels("get_game").Observe(stopwatch.Elapsed.TotalSeconds);
            return Results.Ok(game);
        });

        // ─── POST /api/catalog/games ───────────────────────────
        group.MapPost("/", async (CreateGameRequest request, IGameCatalogRepository repository, ICacheService cacheService, IElasticClient<ElasticCatalog> elasticClient) =>
        {
            var document = new GameDocument
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category,
                Tags = request.Tags ?? new List<string>(),
                Screenshots = request.Screenshots ?? new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            await repository.InsertAsync(document);

            // Invalidar lista do cache
            await cacheService.RemoveAsync("games:all");

            // Adicionar também no Elasticsearch
            try
            {
                var elasticCatalog = new ElasticCatalog
                {
                    Title = document.Name,
                    PriceCents = (decimal)(document.Price * 100),
                    Currency = "BRL"
                };

                await elasticClient.Create(elasticCatalog, "catalog");
            }
            catch (Exception ex)
            {
                // Log do erro, mas não falha a criação do game
                Console.WriteLine($"Erro ao adicionar game no Elasticsearch: {ex.Message}");
            }

            AppMetrics.GamesCreated.Inc();
            return Results.Created($"/api/catalog/games/{document.Id}", ToDto(document));
        });

        // ─── PUT /api/catalog/games/{id} ───────────────────────
        group.MapPut("/{id}", async (string id, UpdateGameRequest request, IGameCatalogRepository repository, ICacheService cacheService, IElasticClient<ElasticCatalog> elasticClient) =>
        {
            var document = await repository.GetByIdAsync(id);
            if (document is null) return Results.NotFound();

            document.Name = request.Name ?? document.Name;
            document.Description = request.Description ?? document.Description;
            document.Price = request.Price ?? document.Price;
            document.Category = request.Category ?? document.Category;
            document.Tags = request.Tags ?? document.Tags;
            document.Screenshots = request.Screenshots ?? document.Screenshots;
            document.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(document);

            // Invalidar cache do jogo e da lista
            await cacheService.RemoveAsync($"games:{id}");
            await cacheService.RemoveAsync("games:all");

            // Atualizar também no Elasticsearch
            try
            {
                var elasticCatalog = new ElasticCatalog
                {
                    Title = document.Name,
                    PriceCents = (decimal)(document.Price * 100),
                    Currency = "BRL"
                };

                await elasticClient.Create(elasticCatalog, "catalog");
            }
            catch (Exception ex)
            {
                // Log do erro, mas não falha a atualização do game
                Console.WriteLine($"Erro ao atualizar game no Elasticsearch: {ex.Message}");
            }

            return Results.Ok(ToDto(document));
        });

        // ─── DELETE /api/catalog/games/{id} ────────────────────
        group.MapDelete("/{id}", async (string id, IGameCatalogRepository repository, ICacheService cacheService) =>
        {
            var deleted = await repository.DeleteAsync(id);
            if (!deleted) return Results.NotFound();

            // Invalidar cache do jogo e da lista
            await cacheService.RemoveAsync($"games:{id}");
            await cacheService.RemoveAsync("games:all");

            return Results.NoContent();
        });
    }

    internal static GameDto ToDto(GameDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Description = document.Description,
        Price = document.Price,
        Category = document.Category,
        Tags = document.Tags,
        Screenshots = document.Screenshots,
        AverageRating = document.AverageRating,
        ReviewCount = document.ReviewCount,
        CreatedAt = document.CreatedAt
    };
}

// ─── DTOs ──────────────────────────────────────────────────────
public class GameDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Screenshots { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public List<string>? Screenshots { get; set; }
}

public class UpdateGameRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Screenshots { get; set; }
}
