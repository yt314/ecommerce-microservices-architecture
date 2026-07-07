using ECommerce.Monolith.Api.DTOs;
using ECommerce.Monolith.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Monolith.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _products;

    public ProductsController(ProductService products) => _products = products;

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request)
    {
        var created = await _products.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
        => Ok(await _products.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var result = await _products.GetByIdAsync(id);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request)
    {
        var result = await _products.UpdateAsync(id, request);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
