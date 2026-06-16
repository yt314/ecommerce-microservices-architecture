namespace OrderService.Entities;

/// <summary>
/// A customer order, owned by OrderService's SQL Server database.
/// Product names/prices are snapshotted onto the order items at order time,
/// fetched over HTTP from ProductCatalogService.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Why an order was rejected (null when confirmed).</summary>
    public string? RejectionReason { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
