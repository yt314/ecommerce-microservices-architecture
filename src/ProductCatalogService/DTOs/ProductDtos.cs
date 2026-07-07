using System.ComponentModel.DataAnnotations;

namespace ProductCatalogService.DTOs;

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

    public Dictionary<string, string> Attributes { get; init; } = new();
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

    public Dictionary<string, string> Attributes { get; init; } = new();
}

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
