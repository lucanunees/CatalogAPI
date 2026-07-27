using Prometheus;
using RedisCache.Library.Extensions;
using CatalogAPI.Endpoints;
using MassTransit.Configuration;
using CatalogAPI.Infrastructure.Persistence;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

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

#region // --- Demais servicos (adicionar conforme necessidade) ---
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

#endregion

app.Run();
