using MongoDB.Bson;
using MongoDB.Driver;
using ProductCatalogService.Models;

namespace ProductCatalogService.Data;

/// <summary>
/// Thin data-access layer over the MongoDB "products" collection.
/// This is the ONLY code that touches the catalog database — no other
/// service has access to it (database-per-service).
/// </summary>
public class ProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public ProductRepository(IMongoDatabase database)
    {
        _products = database.GetCollection<Product>("products");
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _products.InsertOneAsync(product);
        return product; // Id is populated by the driver after insert.
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _products.Find(FilterDefinition<Product>.Empty).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        // Guard against invalid ObjectId strings so we return "not found" instead of throwing.
        if (!ObjectId.TryParse(id, out _))
            return null;

        return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>Replaces an existing product. Returns false if the id does not exist.</summary>
    public async Task<bool> UpdateAsync(string id, Product updated)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        updated.Id = id;
        var result = await _products.ReplaceOneAsync(p => p.Id == id, updated);
        return result.MatchedCount > 0;
    }
}
