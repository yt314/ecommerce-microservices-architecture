using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

/// <summary>EF Core context for the Inventory PostgreSQL database.</summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(i =>
        {
            i.Property(x => x.ProductId).IsRequired().HasMaxLength(100);
            // One inventory row per product.
            i.HasIndex(x => x.ProductId).IsUnique();
        });
    }
}
