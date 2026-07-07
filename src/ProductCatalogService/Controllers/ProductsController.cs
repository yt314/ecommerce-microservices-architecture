using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Data;
using ProductCatalogService.DTOs;
using ProductCatalogService.Models;

namespace ProductCatalogService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductRepository _repository;
    private readonly ProductCache _cache;

    public ProductsController(ProductRepository repository, ProductCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    // Must stay declared before the "{id}" route below, or routing would treat
    // "instance" as an {id} value instead of matching this literal route.
    [HttpGet("instance")]
    public ActionResult<object> Instance()
        => Ok(new { instanceId = Environment.MachineName, service = "ProductCatalogService" });

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = request.IsActive,
            Attributes = request.Attributes
        };

        var created = await _repository.CreateAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetById(string id)
    {
        var product = await _cache.GetByIdAsync(id);
        return product is null
            ? NotFound(new { error = $"Product {id} was not found." })
            : Ok(ToResponse(product));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponse>> Update(string id, UpdateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = request.IsActive,
            Attributes = request.Attributes
        };

        var updated = await _repository.UpdateAsync(id, product);
        if (!updated)
            return NotFound(new { error = $"Product {id} was not found." });

        await _cache.InvalidateAsync(id);

        product.Id = id;
        return Ok(ToResponse(product));
    }

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id ?? string.Empty,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Category = p.Category,
        IsActive = p.IsActive,
        Attributes = p.Attributes
    };
}
