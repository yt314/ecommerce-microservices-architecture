using System.ComponentModel.DataAnnotations;

namespace ECommerce.Monolith.Api.DTOs;

public record CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    [MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public record UpdateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    [MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public record ProductResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
