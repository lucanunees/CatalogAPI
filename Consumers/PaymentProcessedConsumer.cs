using CatalogAPI.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using CatalogAPI.Infrastructure.Persistence;
using Contracts.IntegrationEvents;

namespace CatalogAPI.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEventV1>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(CatalogDbContext db, ILogger<PaymentProcessedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEventV1> context)
    {
        var evt = context.Message;

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == evt.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Order not found: {OrderId}", evt.OrderId);
            return;
        }

        if (evt.Status == "Approved")
        {
            order.Status = OrderStatus.Confirmed;

            _db.LibraryItems.Add(new LibraryItem
            {
                Id = Guid.NewGuid(),
                UserId = order.UserId,
                GameId = order.GameId,
                AcquiredAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Order updated: {OrderId} -> {Status}", order.Id, order.Status);
    }
}