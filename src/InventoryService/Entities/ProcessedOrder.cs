namespace InventoryService.Entities;

/// <summary>
/// Idempotency record: remembers that an OrderPlaced for a given OrderId was
/// already processed, and what the outcome was. If the same message is delivered
/// twice (RabbitMQ is at-least-once), we re-publish the stored outcome instead of
/// reserving stock again.
/// </summary>
public class ProcessedOrder
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Outcome { get; set; } = string.Empty;   // "Reserved" or "Rejected"
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
