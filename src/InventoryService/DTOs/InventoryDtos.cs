using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public record UpdateInventoryRequest
{
    [Range(0, 1_000_000)]
    public int QuantityAvailable { get; init; }

    [Range(0, 1_000_000)]
    public int QuantityReserved { get; init; }
}

// Shared by both the reserve and release endpoints.
public record QuantityRequest
{
    [Range(1, 1_000_000)]
    public int Quantity { get; init; }
}

public record InventoryResponse
{
    public string ProductId { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
}

public record StockOperationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
}
