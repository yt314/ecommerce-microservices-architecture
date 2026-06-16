using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data;

/// <summary>EF Core context for the Order SQL Server database.</summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(o =>
        {
            o.Property(x => x.CustomerEmail).IsRequired().HasMaxLength(256);
            o.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            o.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            o.Property(x => x.RejectionReason).HasMaxLength(1000);

            o.HasMany(x => x.Items)
             .WithOne(x => x.Order!)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(oi =>
        {
            oi.Property(x => x.ProductId).IsRequired().HasMaxLength(100);
            oi.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            oi.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}
