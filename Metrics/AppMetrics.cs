using Prometheus;

namespace CatalogAPI.Metrics;

public static class AppMetrics
{
    // ─── Métricas de Cache ─────────────────────────────────
    public static readonly Counter CacheHits = Prometheus.Metrics.CreateCounter(
        "cache_hits_total",
        "Total de cache hits",
        new CounterConfiguration { LabelNames = new[] { "endpoint" } });

    public static readonly Counter CacheMisses = Prometheus.Metrics.CreateCounter(
        "cache_misses_total",
        "Total de cache misses",
        new CounterConfiguration { LabelNames = new[] { "endpoint" } });

    // ─── Métricas de Negócio ───────────────────────────────
    public static readonly Counter GamesCreated = Prometheus.Metrics.CreateCounter(
        "games_created_total", "Total de jogos criados");

    public static readonly Counter OrdersPlaced = Prometheus.Metrics.CreateCounter(
        "orders_placed_total", "Total de pedidos realizados");

    // --- Métricas de Busca (Elasticsearch) ---
    public static readonly Counter SearchCompleted = Prometheus.Metrics.CreateCounter(
        "search_completed_total",
        "Total de buscas completadas com sucesso",
        new CounterConfiguration { LabelNames = new[] { "type" } });

    public static readonly Counter SearchErrors = Prometheus.Metrics.CreateCounter(
        "search_errors_total",
        "Total de erros nas buscas",
        new CounterConfiguration { LabelNames = new[] { "error_type" } });

    public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
        "business_request_duration_seconds",
        "Duração das operações de negócio",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });
}
