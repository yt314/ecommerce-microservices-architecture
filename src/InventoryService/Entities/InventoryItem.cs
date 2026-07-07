namespace InventoryService.Entities;

// ProductId is a string, not a foreign key: it's ProductCatalogService's
// MongoDB ObjectId. This service stores the id only — it never reads the
// catalog's database directly.
public class InventoryItem
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}
