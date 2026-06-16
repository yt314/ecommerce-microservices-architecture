using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Entities;
using Shared.Messaging;

namespace OrderService.Services;

/// <summary>Result of placing an order: either a Pending order or a validation error.</summary>
public record PlaceOrderResult(OrderResponse? Order, string? ValidationError);

/// <summary>
/// OrderService's part of the choreography saga.
///
/// PlaceOrderAsync (sync, fast):
///   - validate products against ProductCatalogService (fast-fail on bad product)
///   - save a PENDING order with price snapshots
///   - publish OrderPlaced and return immediately
///
/// HandleInventoryReserved/Rejected (async, from the broker):
///   - move the order to Confirmed/Rejected and publish the matching event
///   - idempotent: only acts while the order is still Pending
/// </summary>
public class OrderProcessor
{
    private readonly OrderDbContext _db;
    private readonly ProductCatalogClient _catalog;
    private readonly EventPublisher _publisher;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        OrderDbContext db,
        ProductCatalogClient catalog,
        EventPublisher publisher,
        ILogger<OrderProcessor> logger)
    {
        _db = db;
        _catalog = catalog;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(CreateOrderRequest request)
    {
        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // Synchronous product validation (fast). Inventory is checked later, async.
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (productId, qty) in requested)
        {
            var product = await _catalog.GetProductAsync(productId);
            if (product is null)
                return new PlaceOrderResult(null, $"Product {productId} does not exist.");
            if (!product.IsActive)
                return new PlaceOrderResult(null, $"Product {productId} ('{product.Name}') is not available for purchase.");

            order.Items.Add(new OrderItem
            {
                ProductId = productId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = qty
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} created as Pending; publishing OrderPlaced.", order.Id);

        // Kick off the saga.
        _publisher.Publish(EventBusConstants.OrderPlacedKey, new OrderPlaced(
            Guid.NewGuid(),
            order.Id,
            order.CustomerEmail,
            order.Items.Select(i => new OrderLine(i.ProductId, i.Quantity)).ToList()));

        return new PlaceOrderResult(ToResponse(order), null);
    }

    /// <summary>InventoryReserved → confirm the order. Idempotent on order status.</summary>
    public async Task HandleInventoryReservedAsync(int orderId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) { _logger.LogWarning("InventoryReserved for unknown order {OrderId}.", orderId); return; }
        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation("Order {OrderId} already {Status}; ignoring duplicate InventoryReserved.", orderId, order.Status);
            return;
        }

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} CONFIRMED; publishing OrderConfirmed.", orderId);

        _publisher.Publish(EventBusConstants.OrderConfirmedKey,
            new OrderConfirmed(Guid.NewGuid(), order.Id, order.CustomerEmail, order.TotalAmount));
    }

    /// <summary>InventoryRejected → reject the order. Idempotent on order status.</summary>
    public async Task HandleInventoryRejectedAsync(int orderId, string reason)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) { _logger.LogWarning("InventoryRejected for unknown order {OrderId}.", orderId); return; }
        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation("Order {OrderId} already {Status}; ignoring duplicate InventoryRejected.", orderId, order.Status);
            return;
        }

        order.Status = OrderStatus.Rejected;
        order.RejectionReason = reason;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} REJECTED ({Reason}); publishing OrderRejected.", orderId, reason);

        _publisher.Publish(EventBusConstants.OrderRejectedKey,
            new OrderRejected(Guid.NewGuid(), order.Id, order.CustomerEmail, reason));
    }

    public async Task<List<OrderResponse>> GetAllAsync()
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
        return orders.Select(ToResponse).ToList();
    }

    public async Task<OrderResponse?> GetByIdAsync(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? null : ToResponse(order);
    }

    private static OrderResponse ToResponse(Order o) => new()
    {
        Id = o.Id,
        CustomerEmail = o.CustomerEmail,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        TotalAmount = o.TotalAmount,
        RejectionReason = o.RejectionReason,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
