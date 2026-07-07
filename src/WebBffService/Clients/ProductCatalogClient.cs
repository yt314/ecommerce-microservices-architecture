using System.Net;
using WebBffService.DTOs;

namespace WebBffService.Clients;

// Base address points at the catalog-lb load balancer, not a single replica —
// so BFF reads get spread across catalog instances too.
public class ProductCatalogClient
{
    private readonly HttpClient _http;

    public ProductCatalogClient(HttpClient http) => _http = http;

    public async Task<BackendProduct?> GetProductAsync(string productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BackendProduct>();
    }
}
