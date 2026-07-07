namespace WebBffService.DTOs;

public record BackendOrder(
    int Id,
    string CustomerEmail,
    string Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    string? RejectionReason,
    List<BackendOrderItem> Items);

public record BackendOrderItem(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

public record BackendProduct(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    bool IsActive,
    Dictionary<string, string> Attributes);

public record ProductInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();
}

public record OrderDetailLine
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPriceAtOrder { get; init; }
    public string ProductNameAtOrder { get; init; } = string.Empty;
    public decimal LineTotal { get; init; }

    // Null if the product was since deleted from the catalog; the *AtOrder
    // fields above still preserve what the customer actually bought.
    public ProductInfo? Product { get; init; }
}

public record OrderDetailsResponse
{
    public int OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public decimal TotalAmount { get; init; }
    public string? RejectionReason { get; init; }
    public List<OrderDetailLine> Items { get; init; } = new();
}
