namespace OrderService.Clients;

/// <summary>Result of a reserve/release call to InventoryService.</summary>
public record StockOperationResult(bool Success, string Message, int QuantityAvailable, int QuantityReserved);

/// <summary>Typed HTTP client for InventoryService.</summary>
public class InventoryClient
{
    private readonly HttpClient _http;

    public InventoryClient(HttpClient http) => _http = http;

    /// <summary>Reserve stock. Returns Success=false (no throw) if there isn't enough (409).</summary>
    public async Task<StockOperationResult> ReserveAsync(string productId, int quantity)
    {
        var response = await _http.PostAsJsonAsync($"/api/inventory/{productId}/reserve", new { quantity });
        var result = await response.Content.ReadFromJsonAsync<StockOperationResult>();
        // InventoryService returns 200 on success and 409 on insufficient stock,
        // both with a body — so we just return the parsed result.
        return result ?? new StockOperationResult(false, "No response from InventoryService.", 0, 0);
    }

    /// <summary>Release a previous reservation (best-effort compensation).</summary>
    public async Task ReleaseAsync(string productId, int quantity)
    {
        await _http.PostAsJsonAsync($"/api/inventory/{productId}/release", new { quantity });
    }
}
