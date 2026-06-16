using System.Text.Json;
using NotificationService.DTOs;
using StackExchange.Redis;

namespace NotificationService.Data;

/// <summary>
/// Stores notification records in Redis (a key-value NoSQL store).
/// Layout:
///   - "notification:nextid"        -> integer counter (INCR)
///   - "notification:{id}"          -> JSON of the record
///   - "notifications" (Redis list) -> ids in insertion order
/// This service "sends" notifications by logging them and saving their status;
/// no real email provider is involved (not required for the course).
/// </summary>
public class NotificationStore
{
    private const string IdCounterKey = "notification:nextid";
    private const string IndexKey = "notifications";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationStore> _logger;

    public NotificationStore(IConnectionMultiplexer redis, ILogger<NotificationStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Records a notification triggered by a saga event, idempotently. Uses a
    /// Redis SET-if-not-exists on "notification:processed:{orderId}" so a duplicate
    /// final event for the same order does not create a second notification.
    /// </summary>
    public async Task RecordFromEventAsync(int orderId, string customerEmail, string status, string message, DateTime createdAt)
    {
        var db = _redis.GetDatabase();
        var processedKey = $"notification:processed:{orderId}";

        var isNew = await db.StringSetAsync(processedKey, status, when: When.NotExists);
        if (!isNew)
        {
            _logger.LogInformation("Duplicate final event for order {OrderId}; notification already recorded — skipping.", orderId);
            return;
        }

        await RecordAsync(new CreateNotificationRequest
        {
            CustomerEmail = customerEmail,
            OrderId = orderId.ToString(),
            Status = status,
            Message = message
        }, createdAt);
    }

    public async Task<NotificationRecord> RecordAsync(CreateNotificationRequest request, DateTime createdAt)
    {
        var db = _redis.GetDatabase();

        var id = (await db.StringIncrementAsync(IdCounterKey)).ToString();
        var record = new NotificationRecord
        {
            Id = id,
            CustomerEmail = request.CustomerEmail,
            OrderId = request.OrderId,
            Status = request.Status,
            Message = request.Message,
            CreatedAt = createdAt
        };

        await db.StringSetAsync($"notification:{id}", JsonSerializer.Serialize(record));
        await db.ListRightPushAsync(IndexKey, id);

        // The "fake send": we just log it.
        _logger.LogInformation(
            "NOTIFICATION sent to {Email}: order {OrderId} is {Status}. {Message}",
            record.CustomerEmail, record.OrderId, record.Status, record.Message);

        return record;
    }

    public async Task<List<NotificationRecord>> GetAllAsync()
    {
        var db = _redis.GetDatabase();
        var ids = await db.ListRangeAsync(IndexKey);

        var records = new List<NotificationRecord>();
        foreach (var id in ids)
        {
            var json = await db.StringGetAsync($"notification:{id}");
            if (!json.IsNullOrEmpty)
            {
                var record = JsonSerializer.Deserialize<NotificationRecord>(json!);
                if (record is not null) records.Add(record);
            }
        }
        return records;
    }

    public async Task<NotificationRecord?> GetByIdAsync(string id)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"notification:{id}");
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<NotificationRecord>(json!);
    }
}
