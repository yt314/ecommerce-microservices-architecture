using System.ComponentModel.DataAnnotations;

namespace ECommerce.Monolith.Api.DTOs;

// Absolute quantities, not deltas — simpler for callers than "add 5" / "remove 3".
public record UpdateInventoryRequest
{
    [Range(0, 1_000_000)]
    public int QuantityAvailable { get; init; }

    [Range(0, 1_000_000)]
    public int QuantityReserved { get; init; }
}

public record InventoryResponse
{
    public int ProductId { get; init; }
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
}
