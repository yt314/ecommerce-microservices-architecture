using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductCatalogService.Models;

/// <summary>
/// A product document stored in MongoDB. Unlike the relational Phase 1 entity,
/// this one has a free-form <see cref="Attributes"/> bag so each category can
/// carry its own fields (e.g. a shirt has "size", a laptop has "ram") — this is
/// exactly the flexibility that makes a catalog a good fit for a document DB.
/// </summary>
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

    /// <summary>Optional, category-specific attributes (the "flexible schema").</summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}
