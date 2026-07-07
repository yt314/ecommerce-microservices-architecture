using System.ComponentModel.DataAnnotations;

namespace NotificationService.DTOs;

public record CreateNotificationRequest
{
    [Required, EmailAddress]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required]
    public string OrderId { get; init; } = string.Empty;

    // Expected values are "Confirmed" or "Rejected" — not an enum, since this
    // also round-trips through JSON as a saga event field.
    [Required]
    public string Status { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

public record NotificationRecord
{
    public string Id { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
