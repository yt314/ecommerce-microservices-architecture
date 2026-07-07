using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace InventoryService.Services;

public record OrderReservationResult(bool Reserved, string Reason);

// Each public method does exactly one SaveChanges, so PostgreSQL's own
// transaction is what makes each reserve/release atomic — no explicit
// transaction handling needed here.
public class InventoryManager
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<InventoryManager> _logger;

    public InventoryManager(InventoryDbContext db, ILogger<InventoryManager> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Reserves every line of the order or none — a partial reservation would
    // leave the saga with no clean way to know what to compensate.
    public async Task<OrderReservationResult> ReserveForOrderAsync(int orderId, IReadOnlyList<OrderLine> items)
    {
        // Idempotency guard: have we already handled this order?
        var already = await _db.ProcessedOrders.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (already is not null)
        {
            _logger.LogInformation("Order {OrderId} already processed ({Outcome}); not reserving again.", orderId, already.Outcome);
            return new OrderReservationResult(already.Outcome == "Reserved", already.Reason ?? string.Empty);
        }

        var requested = items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ids = requested.Keys.ToList();

        var stock = await _db.InventoryItems.Where(i => ids.Contains(i.ProductId)).ToListAsync();

        // Check every line first — all-or-nothing.
        var shortages = new List<string>();
        foreach (var (productId, qty) in requested)
        {
            var item = stock.FirstOrDefault(s => s.ProductId == productId);
            var available = item?.QuantityAvailable ?? 0;
            if (available < qty)
                shortages.Add($"product {productId} (requested {qty}, available {available})");
        }

        string outcome;
        string? reason = null;
        if (shortages.Count > 0)
        {
            outcome = "Rejected";
            reason = "Insufficient stock for " + string.Join("; ", shortages);
            _logger.LogWarning("Order {OrderId} REJECTED by inventory: {Reason}. Stock left unchanged.", orderId, reason);
        }
        else
        {
            foreach (var (productId, qty) in requested)
            {
                var item = stock.First(s => s.ProductId == productId);
                item.QuantityAvailable -= qty;
                item.QuantityReserved += qty;
            }
            outcome = "Reserved";
            _logger.LogInformation("Order {OrderId} stock RESERVED.", orderId);
        }

        // The stock change (if any) and the idempotency record commit together.
        _db.ProcessedOrders.Add(new ProcessedOrder
        {
            OrderId = orderId,
            Outcome = outcome,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new OrderReservationResult(outcome == "Reserved", reason ?? string.Empty);
    }

    public async Task<InventoryResponse?> GetAsync(string productId)
    {
        var item = await _db.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        return item is null ? null : ToResponse(item);
    }

    public async Task<InventoryResponse> UpsertAsync(string productId, UpdateInventoryRequest request)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item is null)
        {
            item = new InventoryItem { ProductId = productId };
            _db.InventoryItems.Add(item);
        }

        item.QuantityAvailable = request.QuantityAvailable;
        item.QuantityReserved = request.QuantityReserved;

        await _db.SaveChangesAsync();
        return ToResponse(item);
    }

    public async Task<StockOperationResponse> ReserveAsync(string productId, int quantity)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item is null)
            return Fail($"No inventory exists for product {productId}.");

        if (item.QuantityAvailable < quantity)
            return Fail($"Insufficient stock for product {productId}: requested {quantity}, available {item.QuantityAvailable}.", item);

        item.QuantityAvailable -= quantity;
        item.QuantityReserved += quantity;
        await _db.SaveChangesAsync();

        return Ok("Reserved.", item);
    }

    public async Task<StockOperationResponse> ReleaseAsync(string productId, int quantity)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item is null)
            return Fail($"No inventory exists for product {productId}.");

        var toRelease = Math.Min(quantity, item.QuantityReserved);
        item.QuantityReserved -= toRelease;
        item.QuantityAvailable += toRelease;
        await _db.SaveChangesAsync();

        return Ok("Released.", item);
    }

    private static InventoryResponse ToResponse(InventoryItem i) => new()
    {
        ProductId = i.ProductId,
        QuantityAvailable = i.QuantityAvailable,
        QuantityReserved = i.QuantityReserved
    };

    private static StockOperationResponse Ok(string message, InventoryItem item) => new()
    {
        Success = true,
        Message = message,
        QuantityAvailable = item.QuantityAvailable,
        QuantityReserved = item.QuantityReserved
    };

    private static StockOperationResponse Fail(string message, InventoryItem? item = null) => new()
    {
        Success = false,
        Message = message,
        QuantityAvailable = item?.QuantityAvailable ?? 0,
        QuantityReserved = item?.QuantityReserved ?? 0
    };
}
