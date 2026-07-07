namespace ECommerce.Monolith.Api.Entities;

// QuantityAvailable = still sellable; QuantityReserved = already committed
// to confirmed orders.
public class InventoryItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}
