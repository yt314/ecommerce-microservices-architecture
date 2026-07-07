using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Monolith.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventory;

    public InventoryController(InventoryService inventory) => _inventory = inventory;

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<InventoryResponse>> Get(int productId)
    {
        var result = await _inventory.GetByProductIdAsync(productId);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<InventoryResponse>> Update(int productId, UpdateInventoryRequest request)
    {
        var result = await _inventory.UpdateAsync(productId, request);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
