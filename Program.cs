using Prometheus;
using RedisCache.Library.Extensions;
using CatalogAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// ─── Redis Cache via Kubernetes Secrets ────────────────────────
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

// ─── Demais serviços (adicionar conforme necessidade) ──────────
// builder.Services.AddDbContext<CatalogDbContext>(...);
// builder.Services.AddMassTransit(...);
// builder.Services.AddAuthentication(...);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ─── Swagger ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ─── Prometheus — Métricas HTTP automáticas ────────────────────
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", context => "catalog-api");
});

// ─── Health Check ──────────────────────────────────────────────
app.MapHealthChecks("/health");

// ─── Prometheus — Endpoint /metrics ────────────────────────────
app.MapMetrics();

// ─── Endpoints ─────────────────────────────────────────────────
app.MapGamesEndpoints();

app.Run();
