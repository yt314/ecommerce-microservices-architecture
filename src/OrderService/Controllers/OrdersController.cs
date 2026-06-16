using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

/// <summary>HTTP endpoints for placing and reading orders.</summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderProcessor _orders;

    public OrdersController(OrderProcessor orders) => _orders = orders;

    /// <summary>
    /// Place an order. Always returns 201 with the created order; check the
    /// "status" field — "Confirmed" or "Rejected" (with a rejectionReason).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(CreateOrderRequest request)
    {
        var order = await _orders.PlaceOrderAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>List all orders (newest first).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
        => Ok(await _orders.GetAllAsync());

    /// <summary>Get a single order by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order is null ? NotFound(new { error = $"Order {id} was not found." }) : Ok(order);
    }
}
