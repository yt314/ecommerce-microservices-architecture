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
    /// Place an order. Returns 201 quickly with a PENDING order; the final status
    /// (Confirmed/Rejected) is decided asynchronously via the saga — poll
    /// GET /api/orders/{id} to see it. Returns 422 if a product is invalid.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(CreateOrderRequest request)
    {
        var result = await _orders.PlaceOrderAsync(request);
        if (result.ValidationError is not null)
            return UnprocessableEntity(new { error = result.ValidationError });

        return CreatedAtAction(nameof(GetById), new { id = result.Order!.Id }, result.Order);
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
