namespace Shared.Messaging;

/// <summary>
/// The message contracts exchanged over RabbitMQ for the order saga.
/// They are plain records (immutable, JSON-serializable) shared by the three
/// participating services so producer and consumer always agree on the shape.
/// Every message carries a MessageId and the OrderId, which the consumers use
/// for idempotency.
/// </summary>

/// <summary>One line of an order: which product and how many.</summary>
public record OrderLine(string ProductId, int Quantity);

/// <summary>Published by OrderService once a Pending order is saved.</summary>
public record OrderPlaced(Guid MessageId, int OrderId, string CustomerEmail, List<OrderLine> Items);

/// <summary>Published by InventoryService when stock was successfully reserved.</summary>
public record InventoryReserved(Guid MessageId, int OrderId);

/// <summary>Published by InventoryService when stock could not be reserved.</summary>
public record InventoryRejected(Guid MessageId, int OrderId, string Reason);

/// <summary>Published by OrderService after a successful reservation.</summary>
public record OrderConfirmed(Guid MessageId, int OrderId, string CustomerEmail, decimal TotalAmount);

/// <summary>Published by OrderService after a failed reservation.</summary>
public record OrderRejected(Guid MessageId, int OrderId, string CustomerEmail, string Reason);
