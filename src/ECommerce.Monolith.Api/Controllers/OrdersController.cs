using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Monolith.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orders;

    public OrdersController(OrderService orders) => _orders = orders;

    // 422, not 400: a missing/inactive product or insufficient stock is a
    // failed business rule, not a malformed request.
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(CreateOrderRequest request)
    {
        var result = await _orders.PlaceOrderAsync(request);
        if (result.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

        return UnprocessableEntity(new { error = result.Error });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
        => Ok(await _orders.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var result = await _orders.GetByIdAsync(id);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
