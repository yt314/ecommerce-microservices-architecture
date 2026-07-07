namespace Shared.Messaging;

// Shared by all three saga participants so producer and consumer always agree
// on the message shape. Every message carries MessageId + OrderId, which the
// consumers rely on for idempotency (see RabbitMqConsumerBase).

public record OrderLine(string ProductId, int Quantity);

// OrderPlaced -> InventoryReserved/Rejected -> OrderConfirmed/Rejected is the
// full choreography; each arrow is one of these records.
public record OrderPlaced(Guid MessageId, int OrderId, string CustomerEmail, List<OrderLine> Items);
public record InventoryReserved(Guid MessageId, int OrderId);
public record InventoryRejected(Guid MessageId, int OrderId, string Reason);
public record OrderConfirmed(Guid MessageId, int OrderId, string CustomerEmail, decimal TotalAmount);
public record OrderRejected(Guid MessageId, int OrderId, string CustomerEmail, string Reason);
