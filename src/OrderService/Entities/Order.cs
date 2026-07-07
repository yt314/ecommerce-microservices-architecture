namespace OrderService.Entities;

// ProductName/UnitPrice on each item are snapshotted at order time (fetched
// from ProductCatalogService), so a later price or name change doesn't alter
// the history of an existing order.
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
