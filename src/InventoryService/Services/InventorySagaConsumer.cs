using System.Text.Json;
using Shared.Messaging;

namespace InventoryService.Services;

/// <summary>
/// Consumes OrderPlaced, reserves stock, and publishes InventoryReserved or
/// InventoryRejected. Reads the "inventory.order-placed" queue.
/// </summary>
public class InventorySagaConsumer : RabbitMqConsumerBase
{
    public InventorySagaConsumer(RabbitMqConnection connection, IServiceScopeFactory scopeFactory, ILogger<InventorySagaConsumer> logger)
        : base(connection, scopeFactory, logger) { }

    protected override string QueueName => EventBusConstants.InventoryQueue;

    protected override string[] RoutingKeys => new[] { EventBusConstants.OrderPlacedKey };

    protected override async Task HandleAsync(string routingKey, string body, IServiceProvider services, CancellationToken ct)
    {
        var msg = JsonSerializer.Deserialize<OrderPlaced>(body)!;

        var manager = services.GetRequiredService<InventoryManager>();
        var publisher = services.GetRequiredService<EventPublisher>();

        var result = await manager.ReserveForOrderAsync(msg.OrderId, msg.Items);

        if (result.Reserved)
            publisher.Publish(EventBusConstants.InventoryReservedKey,
                new InventoryReserved(Guid.NewGuid(), msg.OrderId));
        else
            publisher.Publish(EventBusConstants.InventoryRejectedKey,
                new InventoryRejected(Guid.NewGuid(), msg.OrderId, result.Reason));
    }
}
