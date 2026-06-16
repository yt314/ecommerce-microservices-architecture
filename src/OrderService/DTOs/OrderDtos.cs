using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

/// <summary>A single requested line when placing an order.</summary>
public record CreateOrderItemRequest
{
    [Required]
    public string ProductId { get; init; } = string.Empty;

    [Range(1, 10_000)]
    public int Quantity { get; init; }
}

/// <summary>Payload to place an order.</summary>
public record CreateOrderRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

/// <summary>A single order line returned to clients.</summary>
public record OrderItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

/// <summary>An order returned to clients.</summary>
public record OrderResponse
{
    public int Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public decimal TotalAmount { get; init; }
    public string? RejectionReason { get; init; }
    public List<OrderItemResponse> Items { get; init; } = new();
}
