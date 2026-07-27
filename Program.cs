using Prometheus;
using RedisCache.Library.Extensions;
using CatalogAPI.Endpoints;
using CatalogAPI.Infrastructure.Persistence;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using CatalogAPI.Infrastructure.Persistence.Mongo;

var builder = WebApplication.CreateBuilder(args);

#region // --- Redis Cache via Kubernetes Secrets ---

var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "";

var redisConnectionString = string.IsNullOrEmpty(redisPassword)
    ? $"{redisHost}:{redisPort}"
    : $"{redisHost}:{redisPort},password={redisPassword},abortConnect=false";

builder.Services.AddRedisCache(options =>
{
    options.ConnectionString = redisConnectionString;
    options.KeyPrefix = "catalog:";
    options.DefaultExpirationInMinutes = 60;
    options.Enabled = true;
});

#endregion

#region ─── MongoDB (catálogo expandido + avaliações) ─────────────────

var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING")
    ?? builder.Configuration["MongoDb:ConnectionString"]
    ?? "mongodb://localhost:27017";
var mongoDb = Environment.GetEnvironmentVariable("MONGO_DB")
    ?? builder.Configuration["MongoDb:DatabaseName"]
    ?? "catalog";
#endregion

builder.Services.AddSingleton(new CatalogMongoContext(mongoConnectionString, mongoDb));
builder.Services.AddScoped<IGameCatalogRepository, GameCatalogRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

#region ─── Demais serviços (adicionar conforme necessidade) ──────────
// builder.Services.AddDbContext<CatalogDbContext>(...);
// builder.Services.AddMassTransit(...);
// builder.Services.AddAuthentication(...);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

#endregion

#region // --- Elasticsearch Cloud via Kubernetes Secrets ---

var elasticCloudId = Environment.GetEnvironmentVariable("ELASTIC_CLOUD_ID") 
    ?? builder.Configuration["ElasticSettings:CloudId"] 
    ?? throw new InvalidOperationException("ELASTIC_CLOUD_ID nao configurado");

var elasticApiKey = Environment.GetEnvironmentVariable("ELASTIC_API_KEY") 
    ?? builder.Configuration["ElasticSettings:ApiKey"] 
    ?? throw new InvalidOperationException("ELASTIC_API_KEY nao configurado");

builder.Services.AddSingleton<IElasticSettings>(new ElasticSettings
{
    CloudId = elasticCloudId,
    ApiKey = elasticApiKey
});

builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var settings = sp.GetRequiredService<IElasticSettings>();
    var clientSettings = new ElasticsearchClientSettings(
        new Uri(settings.CloudId))
        .Authentication(new ApiKey(settings.ApiKey));

    return new ElasticsearchClient(clientSettings);
});

builder.Services.AddSingleton(typeof(IElasticClient<>), typeof(ElasticClient<>));

#endregion

var app = builder.Build();

#region // --- Swagger ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#endregion

app.UseHttpsRedirection();

#region // --- Prometheus - Metricas HTTP automaticas ---

app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", context => "catalog-api");
});

#endregion

#region // --- Health Check ---

app.MapHealthChecks("/health");

#endregion

#region // --- Prometheus - Endpoint /metrics ---

app.MapMetrics();

#endregion

#region // --- Endpoints ---

app.MapGamesEndpoints();
app.MapSearchEndpoints();
app.MapReviewsEndpoints();

#endregion


app.Run();
