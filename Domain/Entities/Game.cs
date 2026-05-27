namespace CatalogAPI.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}