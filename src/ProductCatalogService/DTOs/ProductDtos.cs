using System.ComponentModel.DataAnnotations;

namespace ProductCatalogService.DTOs;

/// <summary>Payload to create a product.</summary>
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

    /// <summary>Optional category-specific attributes.</summary>
    public Dictionary<string, string> Attributes { get; init; } = new();
}

/// <summary>Payload to update a product (same shape as create).</summary>
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

    public Dictionary<string, string> Attributes { get; init; } = new();
}

/// <summary>Shape returned to clients (including other services).</summary>
public record ProductResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();
}
