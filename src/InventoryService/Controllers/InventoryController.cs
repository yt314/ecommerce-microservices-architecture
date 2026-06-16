using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

/// <summary>HTTP endpoints for inventory (backed by PostgreSQL).</summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryManager _inventory;

    public InventoryController(InventoryManager inventory) => _inventory = inventory;

    /// <summary>Get current stock for a product.</summary>
    [HttpGet("{productId}")]
    public async Task<ActionResult<InventoryResponse>> Get(string productId)
    {
        var result = await _inventory.GetAsync(productId);
        return result is null
            ? NotFound(new { error = $"Inventory for product {productId} was not found." })
            : Ok(result);
    }

    /// <summary>Set/update available + reserved quantities (creates the row if missing).</summary>
    [HttpPut("{productId}")]
    public async Task<ActionResult<InventoryResponse>> Upsert(string productId, UpdateInventoryRequest request)
        => Ok(await _inventory.UpsertAsync(productId, request));

    /// <summary>Reserve stock for a product. Returns 409 if there is not enough.</summary>
    [HttpPost("{productId}/reserve")]
    public async Task<ActionResult<StockOperationResponse>> Reserve(string productId, QuantityRequest request)
    {
        var result = await _inventory.ReserveAsync(productId, request.Quantity);
        return result.Success ? Ok(result) : Conflict(result);
    }

    /// <summary>Release a previous reservation (compensation helper).</summary>
    [HttpPost("{productId}/release")]
    public async Task<ActionResult<StockOperationResponse>> Release(string productId, QuantityRequest request)
        => Ok(await _inventory.ReleaseAsync(productId, request.Quantity));
}
