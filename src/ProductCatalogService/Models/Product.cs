using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductCatalogService.Models;

// Attributes is a free-form bag so each category can carry its own fields
// (a shirt has "size", a laptop has "ram") without a schema migration.
public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Store money as Decimal128 so we keep exact decimal precision in BSON.
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Dictionary<string, string> Attributes { get; set; } = new();
}
