using Microsoft.AspNetCore.Mvc;
using WebBffService.Clients;
using WebBffService.DTOs;

namespace WebBffService.Controllers;

// "Order details" isn't one backend resource — it combines OrderService and
// ProductCatalogService into one response shaped for this client. That
// composition logic belongs here, not in the gateway (which only routes).
[ApiController]
[Route("api/order-details")]
public class OrderDetailsController : ControllerBase
{
    private readonly OrderClient _orders;
    private readonly ProductCatalogClient _catalog;

    public OrderDetailsController(OrderClient orders, ProductCatalogClient catalog)
    {
        _orders = orders;
        _catalog = catalog;
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailsResponse>> Get(int orderId)
    {
        var order = await _orders.GetOrderAsync(orderId);
        if (order is null)
            return NotFound(new { error = $"Order {orderId} was not found." });

        var lines = new List<OrderDetailLine>();
        foreach (var item in order.Items)
        {
            var product = await _catalog.GetProductAsync(item.ProductId);
            lines.Add(new OrderDetailLine
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPriceAtOrder = item.UnitPrice,
                ProductNameAtOrder = item.ProductName,
                LineTotal = item.UnitPrice * item.Quantity,
                Product = product is null ? null : new ProductInfo
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    CurrentPrice = product.Price,
                    Category = product.Category,
                    IsActive = product.IsActive,
                    Attributes = product.Attributes
                }
            });
        }

        return Ok(new OrderDetailsResponse
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            RejectionReason = order.RejectionReason,
            Items = lines
        });
    }
}
