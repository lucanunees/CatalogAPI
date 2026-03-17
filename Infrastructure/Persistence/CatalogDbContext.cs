using CatalogAPI.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MassTransit.EntityFrameworkCoreIntegration;

namespace CatalogAPI.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LibraryItem> LibraryItems => Set<LibraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(b =>
        {
            b.ToTable("Games");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.Title).IsUnique(false);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.GameId);
            b.Property(x => x.Currency).HasMaxLength(8);
        });

        modelBuilder.Entity<LibraryItem>(b =>
        {
            b.ToTable("LibraryItems");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.GameId }).IsUnique(); 
        });
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}