namespace Shared.Messaging;

public static class EventBusConstants
{
    // A single topic exchange for every saga event; routing keys (below) do the
    // discrimination, so a new event type never needs a new exchange.
    public const string Exchange = "ecommerce.events";

    public const string OrderPlacedKey = "order.placed";
    public const string InventoryReservedKey = "inventory.reserved";
    public const string InventoryRejectedKey = "inventory.rejected";
    public const string OrderConfirmedKey = "order.confirmed";
    public const string OrderRejectedKey = "order.rejected";

    public const string InventoryQueue = "inventory.order-placed";
    public const string OrderResultsQueue = "order.inventory-results";
    public const string NotificationQueue = "notification.order-final";
}
