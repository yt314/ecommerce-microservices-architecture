namespace ECommerce.Monolith.Api.Entities;

// TotalAmount and Status are computed by OrderService when the order is
// placed — never taken directly from the client request.
public class Order
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
