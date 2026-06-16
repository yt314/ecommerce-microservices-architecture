using System.Text.Json;
using Shared.Messaging;

namespace OrderService.Services;

/// <summary>
/// Consumes inventory results and drives the order to its final state.
/// Reads the "order.inventory-results" queue (InventoryReserved / InventoryRejected).
/// </summary>
public class OrderSagaConsumer : RabbitMqConsumerBase
{
    public OrderSagaConsumer(RabbitMqConnection connection, IServiceScopeFactory scopeFactory, ILogger<OrderSagaConsumer> logger)
        : base(connection, scopeFactory, logger) { }

    protected override string QueueName => EventBusConstants.OrderResultsQueue;

    protected override string[] RoutingKeys => new[]
    {
        EventBusConstants.InventoryReservedKey,
        EventBusConstants.InventoryRejectedKey
    };

    protected override async Task HandleAsync(string routingKey, string body, IServiceProvider services, CancellationToken ct)
    {
        var processor = services.GetRequiredService<OrderProcessor>();

        if (routingKey == EventBusConstants.InventoryReservedKey)
        {
            var msg = JsonSerializer.Deserialize<InventoryReserved>(body)!;
            await processor.HandleInventoryReservedAsync(msg.OrderId);
        }
        else if (routingKey == EventBusConstants.InventoryRejectedKey)
        {
            var msg = JsonSerializer.Deserialize<InventoryRejected>(body)!;
            await processor.HandleInventoryRejectedAsync(msg.OrderId, msg.Reason);
        }
    }
}
