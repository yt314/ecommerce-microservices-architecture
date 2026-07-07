using System.Net;
using WebBffService.DTOs;

namespace WebBffService.Clients;

public class OrderClient
{
    private readonly HttpClient _http;

    public OrderClient(HttpClient http) => _http = http;

    public async Task<BackendOrder?> GetOrderAsync(int orderId)
    {
        var response = await _http.GetAsync($"/api/orders/{orderId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BackendOrder>();
    }
}
