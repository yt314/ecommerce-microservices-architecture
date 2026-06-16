using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

/// <summary>
/// Business logic for inventory: read, set (upsert), reserve and release stock.
/// Each public method does a single SaveChanges, which PostgreSQL executes in
/// its own transaction — so individual reserve/release operations are atomic.
/// </summary>
public class InventoryManager
{
    private readonly InventoryDbContext _db;

    public InventoryManager(InventoryDbContext db) => _db = db;

    public async Task<InventoryResponse?> GetAsync(string productId)
    {
        var item = await _db.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        return item is null ? null : ToResponse(item);
    }

    /// <summary>Creates the inventory row if missing, otherwise updates it.</summary>
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

    /// <summary>
    /// Reserve stock: available goes down, reserved goes up.
    /// Fails (Success=false) if the product has no inventory row or not enough stock.
    /// </summary>
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

    /// <summary>
    /// Release a previous reservation: reserved goes down, available goes back up.
    /// Used as a best-effort compensation if a multi-item order fails partway.
    /// </summary>
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
