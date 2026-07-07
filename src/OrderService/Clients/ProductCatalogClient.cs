using System.Net;

namespace OrderService.Clients;

// Only the fields OrderService actually needs from ProductCatalogService's response.
public record CatalogProduct(string Id, string Name, decimal Price, bool IsActive);

// Database-per-service: OrderService reaches the catalog only over HTTP,
// never its MongoDB directly.
public class ProductCatalogClient
{
    private readonly HttpClient _http;

    public ProductCatalogClient(HttpClient http) => _http = http;

    // Maps a 404 to null instead of throwing, so callers can treat "product
    // doesn't exist" as a normal case.
    public async Task<CatalogProduct?> GetProductAsync(string productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProduct>();
    }
}
