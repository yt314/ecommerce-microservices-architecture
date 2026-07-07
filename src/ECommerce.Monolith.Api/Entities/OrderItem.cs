namespace ECommerce.Monolith.Api.Entities;

// ProductName/UnitPrice are snapshotted at order time so the order stays
// correct even if the product is later renamed or repriced.
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
