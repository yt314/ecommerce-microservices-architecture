using ECommerce.Monolith.Api.Data;
using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Monolith.Api.Services;

public class InventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<InventoryResponse>> GetByProductIdAsync(int productId)
    {
        var item = await _db.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        return item is null
            ? ServiceResult<InventoryResponse>.NotFound($"Inventory for product {productId} was not found.")
            : ServiceResult<InventoryResponse>.Success(ToResponse(item));
    }

    // Upsert: creates the inventory row if this product doesn't have one yet.
    public async Task<ServiceResult<InventoryResponse>> UpdateAsync(int productId, UpdateInventoryRequest request)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
            return ServiceResult<InventoryResponse>.NotFound($"Product {productId} was not found.");

        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item is null)
        {
            item = new InventoryItem { ProductId = productId };
            _db.InventoryItems.Add(item);
        }

        item.QuantityAvailable = request.QuantityAvailable;
        item.QuantityReserved = request.QuantityReserved;

        await _db.SaveChangesAsync();
        return ServiceResult<InventoryResponse>.Success(ToResponse(item));
    }

    private static InventoryResponse ToResponse(InventoryItem i) => new()
    {
        ProductId = i.ProductId,
        QuantityAvailable = i.QuantityAvailable,
        QuantityReserved = i.QuantityReserved
    };
}
