using CatalogAPI.Domain.Elastic;
using CatalogAPI.Infrastructure.Persistence;
using Elastic.Clients.Elasticsearch;

using Elastic.Transport;
using CatalogAPI.Metrics;

namespace CatalogAPI.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog/search")
            .WithTags("Search");

        // --- GET /api/catalog/search/products?q=query ---
        group.MapGet("/catalog", SearchProducts)
            .WithName("SearchProducts")
            .WithOpenApi()
            .Produces<SearchResponseDto>()
            .Produces(400)
            .WithSummary("Pesquisar games no catálogo.")
            .WithDescription("Realiza busca com match query com boost de relevância e fuzzy para tolerar erros de digitação");
    }

    /// <summary>
    /// Busca produtos no Elasticsearch com:
    /// - Match query com boost em campos específicos (título tem peso maior)
    /// - Fuzzy matching para tolerar pequenos erros de digitação
    /// - Índice padrão: "catalog"
    /// </summary>
    private static async Task<IResult> SearchProducts(
        string nome,
        IElasticSettings elasticSettings)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Validações
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Results.BadRequest(new { error = "Query parameter 'nome' é obrigatório e não pode estar vazio" });
        }

        if (nome.Length > 200)
        {
            return Results.BadRequest(new { error = "Query não pode exceder 200 caracteres" });
        }

        try
        {
            // Criar cliente Elasticsearch
            var clientSettings = new ElasticsearchClientSettings(
                new Uri(elasticSettings.CloudId))
                .Authentication(new ApiKey(elasticSettings.ApiKey));

            var client = new ElasticsearchClient(clientSettings);
            var indexName = "catalog";

            // Construir a query com Match + Fuzziness com paginação
            var searchResponse = await client.SearchAsync<ElasticCatalog>(s => s
                .Index(indexName)
                .Query(query => query
                    .Match(m => m
                        .Field("Title")
                        .Query(nome)
                        .Fuzziness("AUTO")
                    )
                )
            );

            if (!searchResponse.IsValidResponse)
            {
                AppMetrics.SearchErrors.WithLabels("elasticsearch_error").Inc();
                stopwatch.Stop();
                return Results.BadRequest(new
                {
                    error = "Erro ao buscar no Elasticsearch",
                    details = searchResponse.ApiCallDetails?.DebugInformation
                });
            }

            // Mapear resultados com highlights
            var results = new List<SearchResultDto>();

            var docs = searchResponse.Documents.ToList();
            var hits = searchResponse.Hits.ToList();

            for (int i = 0; i < docs.Count; i++)
            {
                var doc = docs[i];
                var hit = hits[i];

                results.Add(new SearchResultDto
                {
                    Title = doc.Title,
                    PriceCents = doc.PriceCents,
                    Currency = doc.Currency,
                    Score = hit.Score ?? 0
                });
            }

            stopwatch.Stop();

            AppMetrics.SearchCompleted.WithLabels("elasticsearch_search").Inc();
            AppMetrics.RequestDuration.WithLabels("search_products").Observe(stopwatch.Elapsed.TotalSeconds);

            var response = new SearchResponseDto
            {
                Query = nome,
                TotalResults = searchResponse.Total,
                Results = results,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AppMetrics.SearchErrors.WithLabels("general_error").Inc();

            return Results.StatusCode(500);
        }
    }
}

/// <summary>
/// DTO para resultado de busca individual
/// </summary>
public class SearchResultDto
{
    public string Title { get; set; } = string.Empty;
    public decimal PriceCents { get; set; }
    public string Currency { get; set; } = "BRL";

    /// <summary>
    /// Score de relevância do Elasticsearch (0+, quanto maior, mais relevante)
    /// </summary>
    public double Score { get; set; }
}

/// <summary>
/// DTO para resposta de busca
/// </summary>
public class SearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public long TotalResults { get; set; }
    public List<SearchResultDto> Results { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
}
