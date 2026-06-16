namespace OrderService.Entities;

/// <summary>Lifecycle states for an order (same set as Phase 1).</summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Rejected
}
