using ECommerce.Monolith.Api.Data;
using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Monolith.Api.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = request.IsActive,
            // Every product starts with an empty inventory record.
            Inventory = new InventoryItem { QuantityAvailable = 0, QuantityReserved = 0 }
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return ToResponse(product);
    }

    public async Task<List<ProductResponse>> GetAllAsync()
    {
        var products = await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();

        return products.Select(ToResponse).ToList();
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(int id)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null
            ? ServiceResult<ProductResponse>.NotFound($"Product {id} was not found.")
            : ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return ServiceResult<ProductResponse>.NotFound($"Product {id} was not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Category = p.Category,
        IsActive = p.IsActive
    };
}
