using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Entities;

namespace OrderService.Services;

/// <summary>
/// Orchestrates placing an order across services over synchronous HTTP:
///   1. Validate every product via ProductCatalogService.
///   2. Reserve stock for every line via InventoryService.
///   3. Persist a Confirmed (or Rejected) order in OrderService's own database.
///   4. Record a notification via NotificationService.
///
/// If a later reservation fails, we release the earlier ones (best-effort
/// compensation). Full event-driven saga compensation arrives in Phase 4 —
/// this stays deliberately simple and synchronous for Phase 2.
/// </summary>
public class OrderProcessor
{
    private readonly OrderDbContext _db;
    private readonly ProductCatalogClient _catalog;
    private readonly InventoryClient _inventory;
    private readonly NotificationClient _notifications;

    public OrderProcessor(
        OrderDbContext db,
        ProductCatalogClient catalog,
        InventoryClient inventory,
        NotificationClient notifications)
    {
        _db = db;
        _catalog = catalog;
        _inventory = inventory;
        _notifications = notifications;
    }

    public async Task<OrderResponse> PlaceOrderAsync(CreateOrderRequest request)
    {
        // Merge duplicate product lines.
        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // --- Step 1: validate all products against the catalog ---
        var products = new Dictionary<string, CatalogProduct>();
        foreach (var productId in requested.Keys)
        {
            var product = await _catalog.GetProductAsync(productId);
            if (product is null)
                return await RejectAsync(request, $"Product {productId} does not exist.");
            if (!product.IsActive)
                return await RejectAsync(request, $"Product {productId} ('{product.Name}') is not available for purchase.");
            products[productId] = product;
        }

        // --- Step 2: reserve stock for every line (compensate on failure) ---
        var reserved = new List<(string productId, int qty)>();
        foreach (var (productId, qty) in requested)
        {
            var result = await _inventory.ReserveAsync(productId, qty);
            if (!result.Success)
            {
                // Roll back the reservations we already made.
                foreach (var (rid, rqty) in reserved)
                    await _inventory.ReleaseAsync(rid, rqty);

                return await RejectAsync(request, result.Message);
            }
            reserved.Add((productId, qty));
        }

        // --- Step 3: all good — persist a Confirmed order ---
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var (productId, qty) in requested)
        {
            var product = products[productId];
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

        // --- Step 4: notify the customer ---
        await _notifications.NotifyAsync(order.CustomerEmail, order.Id.ToString(), "Confirmed",
            $"Your order #{order.Id} has been confirmed. Total: {order.TotalAmount:0.00}.");

        return ToResponse(order);
    }

    /// <summary>Persists a Rejected order (no line snapshots needed) and notifies.</summary>
    private async Task<OrderResponse> RejectAsync(CreateOrderRequest request, string reason)
    {
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Rejected,
            CreatedAt = DateTime.UtcNow,
            RejectionReason = reason,
            TotalAmount = 0
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _notifications.NotifyAsync(order.CustomerEmail, order.Id.ToString(), "Rejected",
            $"Your order #{order.Id} was rejected: {reason}");

        return ToResponse(order);
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
