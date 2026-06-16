using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

/// <summary>EF Core context for the Inventory PostgreSQL database.</summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ProcessedOrder> ProcessedOrders => Set<ProcessedOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(i =>
        {
            i.Property(x => x.ProductId).IsRequired().HasMaxLength(100);
            // One inventory row per product.
            i.HasIndex(x => x.ProductId).IsUnique();
        });

        modelBuilder.Entity<ProcessedOrder>(p =>
        {
            p.Property(x => x.Outcome).IsRequired().HasMaxLength(20);
            p.Property(x => x.Reason).HasMaxLength(1000);
            // Each order is processed once → unique guard.
            p.HasIndex(x => x.OrderId).IsUnique();
        });
    }
}
