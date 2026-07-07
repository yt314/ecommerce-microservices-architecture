using ECommerce.Monolith.Api.Data;
using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Monolith.Api.Services;

// Placement rules:
//   1. Every requested product must exist and be active.
//   2. There must be enough available inventory for every line.
//   3. On success: stock moves from available to reserved and a Confirmed
//      order is created, all in a single transaction.
//   4. On failure: nothing is persisted.
public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<OrderResponse>> PlaceOrderAsync(CreateOrderRequest request)
    {
        // Merge duplicate product lines so quantity checks are correct
        // even if the client sends the same product twice.
        var requestedQuantities = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var productIds = requestedQuantities.Keys.ToList();

        // EF Core's retrying execution strategy (EnableRetryOnFailure) does not
        // allow a user-initiated transaction on its own — the whole transactional
        // unit must be wrapped so it can be retried as one block on transient
        // failures. We therefore run the entire read/validate/write/commit flow
        // inside the strategy's ExecuteAsync, and BeginTransaction lives inside it.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var products = await _db.Products
                .Include(p => p.Inventory)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Rule 1: all requested products must exist.
            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
                return ServiceResult<OrderResponse>.Validation(
                    $"These products do not exist: {string.Join(", ", missing)}.");

            // Rule 1b: inactive products cannot be ordered.
            var inactive = products.Where(p => !p.IsActive).Select(p => p.Id).ToList();
            if (inactive.Count > 0)
                return ServiceResult<OrderResponse>.Validation(
                    $"These products are not available for purchase: {string.Join(", ", inactive)}.");

            // Rule 2: enough available inventory for every line.
            var shortages = new List<string>();
            foreach (var product in products)
            {
                var requested = requestedQuantities[product.Id];
                var available = product.Inventory?.QuantityAvailable ?? 0;
                if (available < requested)
                    shortages.Add($"Product {product.Id} ('{product.Name}'): requested {requested}, available {available}");
            }

            if (shortages.Count > 0)
                return ServiceResult<OrderResponse>.Validation(
                    "Insufficient inventory. " + string.Join("; ", shortages));

            // All checks passed — build the confirmed order inside a transaction so
            // the stock change and the order row commit together (all or nothing).
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var product in products)
            {
                var quantity = requestedQuantities[product.Id];

                // Rule 3: reserve the stock — available goes down, reserved goes up.
                product.Inventory!.QuantityAvailable -= quantity;
                product.Inventory.QuantityReserved += quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,   // snapshot the name/price at order time
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<OrderResponse>.Success(ToResponse(order));
        });
    }

    public async Task<List<OrderResponse>> GetAllAsync()
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return orders.Select(ToResponse).ToList();
    }

    public async Task<ServiceResult<OrderResponse>> GetByIdAsync(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null
            ? ServiceResult<OrderResponse>.NotFound($"Order {id} was not found.")
            : ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order o) => new()
    {
        Id = o.Id,
        CustomerEmail = o.CustomerEmail,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        TotalAmount = o.TotalAmount,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
