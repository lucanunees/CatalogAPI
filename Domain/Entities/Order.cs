namespace CatalogAPI.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;
}