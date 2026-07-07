using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderProcessor _orders;

    public OrdersController(OrderProcessor orders) => _orders = orders;

    // Returns 201 with a Pending order immediately; Confirmed/Rejected is decided
    // asynchronously by the saga, so callers need to poll GetById for the outcome.
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place(CreateOrderRequest request)
    {
        var result = await _orders.PlaceOrderAsync(request);
        if (result.ValidationError is not null)
            return UnprocessableEntity(new { error = result.ValidationError });

        return CreatedAtAction(nameof(GetById), new { id = result.Order!.Id }, result.Order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
        => Ok(await _orders.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order is null ? NotFound(new { error = $"Order {id} was not found." }) : Ok(order);
    }
}
