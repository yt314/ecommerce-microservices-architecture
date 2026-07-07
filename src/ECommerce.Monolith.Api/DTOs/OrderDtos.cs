using System.ComponentModel.DataAnnotations;

namespace ECommerce.Monolith.Api.DTOs;

public record CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10_000)]
    public int Quantity { get; init; }
}

public record CreateOrderRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

public record OrderItemResponse
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public record OrderResponse
{
    public int Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemResponse> Items { get; init; } = new();
}
