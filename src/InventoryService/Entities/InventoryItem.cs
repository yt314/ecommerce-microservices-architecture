namespace InventoryService.Entities;

/// <summary>
/// Stock record for one product. ProductId is a string because product ids now
/// come from ProductCatalogService (MongoDB ObjectId). This service only stores
/// the id — it never reads the catalog's database.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}
