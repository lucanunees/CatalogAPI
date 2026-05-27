using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.API.Endpoints;

public static class GamesEndpoints
{
    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", async (CatalogDbContext db) =>
            await db.Games.AsNoTracking().OrderBy(x => x.Title).ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, CatalogDbContext db) =>
        {
            var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return game is null ? Results.NotFound() : Results.Ok(game);
        });

        // Admin-only (você pode testar com token de admin mais tarde)
        group.MapPost("/", [Authorize(Roles = "Admin")] async (Game req, CatalogDbContext db) =>
        {
            req.Id = Guid.NewGuid();
            req.CreatedAtUtc = DateTime.UtcNow;
            db.Games.Add(req);
            await db.SaveChangesAsync();
            return Results.Created($"/games/{req.Id}", req);
        });

        group.MapPut("/{id:guid}", [Authorize(Roles = "Admin")] async (Guid id, Game req, CatalogDbContext db) =>
        {
            var game = await db.Games.FirstOrDefaultAsync(x => x.Id == id);
            if (game is null) return Results.NotFound();

            game.Title = req.Title;
            game.PriceCents = req.PriceCents;
            game.Currency = req.Currency;

            await db.SaveChangesAsync();
            return Results.Ok(game);
        });

        group.MapDelete("/{id:guid}", [Authorize(Roles = "Admin")] async (Guid id, CatalogDbContext db) =>
        {
            var game = await db.Games.FirstOrDefaultAsync(x => x.Id == id);
            if (game is null) return Results.NotFound();

            db.Games.Remove(game);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}