namespace OrderService.Clients;

/// <summary>Typed HTTP client for NotificationService.</summary>
public class NotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient http, ILogger<NotificationClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Records a notification. Failure here must NOT fail the order, so we
    /// swallow and log errors (the order outcome is already decided).
    /// </summary>
    public async Task NotifyAsync(string customerEmail, string orderId, string status, string message)
    {
        try
        {
            await _http.PostAsJsonAsync("/api/notifications",
                new { customerEmail, orderId, status, message });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification for order {OrderId}.", orderId);
        }
    }
}
