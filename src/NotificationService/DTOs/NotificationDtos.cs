using System.ComponentModel.DataAnnotations;

namespace NotificationService.DTOs;

/// <summary>Payload OrderService sends to record a notification.</summary>
public record CreateNotificationRequest
{
    [Required, EmailAddress]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required]
    public string OrderId { get; init; } = string.Empty;

    /// <summary>Final order state, e.g. "Confirmed" or "Rejected".</summary>
    [Required]
    public string Status { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>A stored notification record.</summary>
public record NotificationRecord
{
    public string Id { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
