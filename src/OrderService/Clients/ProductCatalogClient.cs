using System.Net;

namespace OrderService.Clients;

/// <summary>Product data as returned by ProductCatalogService (the parts we need).</summary>
public record CatalogProduct(string Id, string Name, decimal Price, bool IsActive);

/// <summary>
/// Typed HTTP client for ProductCatalogService. OrderService never touches the
/// catalog database directly — it only asks over HTTP.
/// </summary>
public class ProductCatalogClient
{
    private readonly HttpClient _http;

    public ProductCatalogClient(HttpClient http) => _http = http;

    /// <summary>Returns the product, or null if it does not exist (404).</summary>
    public async Task<CatalogProduct?> GetProductAsync(string productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProduct>();
    }
}
