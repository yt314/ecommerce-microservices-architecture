namespace Shared.Messaging;

/// <summary>Central names for the exchange, routing keys and queues.</summary>
public static class EventBusConstants
{
    /// <summary>One durable topic exchange carries every saga event.</summary>
    public const string Exchange = "ecommerce.events";

    // Routing keys (also used as the message "type" discriminator on shared queues).
    public const string OrderPlacedKey = "order.placed";
    public const string InventoryReservedKey = "inventory.reserved";
    public const string InventoryRejectedKey = "inventory.rejected";
    public const string OrderConfirmedKey = "order.confirmed";
    public const string OrderRejectedKey = "order.rejected";

    // Durable queues, one per consuming service.
    public const string InventoryQueue = "inventory.order-placed";
    public const string OrderResultsQueue = "order.inventory-results";
    public const string NotificationQueue = "notification.order-final";
}
