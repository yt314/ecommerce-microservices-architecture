namespace ECommerce.Monolith.Api.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    // Soft on/off switch so a product can be hidden without deleting it
    // (and losing its order history).
    public bool IsActive { get; set; } = true;

    public InventoryItem? Inventory { get; set; }
}
