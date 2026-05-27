using CatalogAPI.Infrastructure.Auth;
using CatalogAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.API.Endpoints;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/library").RequireAuthorization();

        group.MapGet("/", async (CatalogDbContext db, HttpContext http) =>
        {
            var userId = UserContext.GetUserId(http.User);

            var items = await db.LibraryItems.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.AcquiredAtUtc)
                .ToListAsync();

            return Results.Ok(items);
        });

        return app;
    }
}