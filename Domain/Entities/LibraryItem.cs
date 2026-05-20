namespace CatalogAPI.Domain.Entities;

public class LibraryItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;
}