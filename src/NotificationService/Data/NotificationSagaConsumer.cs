using System.Text.Json;
using Shared.Messaging;

namespace NotificationService.Data;

/// <summary>
/// Consumes the final order events and records a notification in Redis.
/// Reads the "notification.order-final" queue (OrderConfirmed / OrderRejected).
/// </summary>
public class NotificationSagaConsumer : RabbitMqConsumerBase
{
    public NotificationSagaConsumer(RabbitMqConnection connection, IServiceScopeFactory scopeFactory, ILogger<NotificationSagaConsumer> logger)
        : base(connection, scopeFactory, logger) { }

    protected override string QueueName => EventBusConstants.NotificationQueue;

    protected override string[] RoutingKeys => new[]
    {
        EventBusConstants.OrderConfirmedKey,
        EventBusConstants.OrderRejectedKey
    };

    protected override async Task HandleAsync(string routingKey, string body, IServiceProvider services, CancellationToken ct)
    {
        var store = services.GetRequiredService<NotificationStore>();
        var now = DateTime.UtcNow;

        if (routingKey == EventBusConstants.OrderConfirmedKey)
        {
            var msg = JsonSerializer.Deserialize<OrderConfirmed>(body)!;
            await store.RecordFromEventAsync(msg.OrderId, msg.CustomerEmail, "Confirmed",
                $"Your order #{msg.OrderId} has been confirmed. Total: {msg.TotalAmount:0.00}.", now);
        }
        else if (routingKey == EventBusConstants.OrderRejectedKey)
        {
            var msg = JsonSerializer.Deserialize<OrderRejected>(body)!;
            await store.RecordFromEventAsync(msg.OrderId, msg.CustomerEmail, "Rejected",
                $"Your order #{msg.OrderId} was rejected: {msg.Reason}", now);
        }
    }
}
