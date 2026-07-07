namespace InventoryService.Entities;

// RabbitMQ only guarantees at-least-once delivery, so OrderPlaced can arrive
// twice for the same order. This table is what makes InventoryManager
// idempotent: a repeat delivery re-publishes the stored outcome instead of
// reserving stock a second time.
public class ProcessedOrder
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Outcome { get; set; } = string.Empty;   // "Reserved" or "Rejected"
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
