using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryManager _inventory;

    public InventoryController(InventoryManager inventory) => _inventory = inventory;

    [HttpGet("{productId}")]
    public async Task<ActionResult<InventoryResponse>> Get(string productId)
    {
        var result = await _inventory.GetAsync(productId);
        return result is null
            ? NotFound(new { error = $"Inventory for product {productId} was not found." })
            : Ok(result);
    }

    // Upsert: creates the row if this productId has none yet.
    [HttpPut("{productId}")]
    public async Task<ActionResult<InventoryResponse>> Upsert(string productId, UpdateInventoryRequest request)
        => Ok(await _inventory.UpsertAsync(productId, request));

    // 409, not 400: the request is well-formed, there just isn't enough stock.
    [HttpPost("{productId}/reserve")]
    public async Task<ActionResult<StockOperationResponse>> Reserve(string productId, QuantityRequest request)
    {
        var result = await _inventory.ReserveAsync(productId, request.Quantity);
        return result.Success ? Ok(result) : Conflict(result);
    }

    [HttpPost("{productId}/release")]
    public async Task<ActionResult<StockOperationResponse>> Release(string productId, QuantityRequest request)
        => Ok(await _inventory.ReleaseAsync(productId, request.Quantity));
}
