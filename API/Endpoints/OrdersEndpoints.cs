using CatalogAPI.Domain.Entities;
using CatalogAPI.Infrastructure.Auth;
using CatalogAPI.Infrastructure.Persistence;
using Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.API.Endpoints;

public static class OrdersEndpoints
{
    public record PlaceOrderRequest(Guid GameId);

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").RequireAuthorization();

        group.MapPost("/", PlaceOrderAsync);

        group.MapGet("/", async (CatalogDbContext db, HttpContext http) =>
        {
            var userId = UserContext.GetUserId(http.User);
            var orders = await db.Orders.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.PlacedAtUtc)
                .ToListAsync();

            return Results.Ok(orders);
        });

        return app;
    }

    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderRequest req,
        CatalogDbContext db,
        IPublishEndpoint publishEndpoint,
        HttpContext http
    )
    {
        var userId = UserContext.GetUserId(http.User);

        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.GameId);
        if (game is null) return Results.NotFound(new { message = "Game not found" });
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = game.Id,
            PriceCents = game.PriceCents,
            Currency = game.Currency,
            Status = OrderStatus.Pending,
            PlacedAtUtc = DateTime.UtcNow
        };

        db.Orders.Add(order);

        var evt = new OrderPlacedEventV1(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            OrderId: order.Id,
            UserId: order.UserId,
            GameId: order.GameId,
            PriceCents: order.PriceCents,
            Currency: order.Currency
        );

        await publishEndpoint.Publish(evt, ctx =>
        {
            ctx.SetRoutingKey("v1.order-placed");
            ctx.CorrelationId = evt.EventId;
        });
        
        await db.SaveChangesAsync();

        return Results.Accepted($"/orders/{order.Id}", new
        {
            order.Id,
            order.Status,
            evt.EventId
        });
    }
}