using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

/// <summary>Set/upsert absolute available + reserved quantities for a product.</summary>
public record UpdateInventoryRequest
{
    [Range(0, 1_000_000)]
    public int QuantityAvailable { get; init; }

    [Range(0, 1_000_000)]
    public int QuantityReserved { get; init; }
}

/// <summary>Reserve (or release) a number of units of a product.</summary>
public record QuantityRequest
{
    [Range(1, 1_000_000)]
    public int Quantity { get; init; }
}

/// <summary>Current stock for a product.</summary>
public record InventoryResponse
{
    public string ProductId { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
}

/// <summary>Result of a reserve/release attempt.</summary>
public record StockOperationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
}
