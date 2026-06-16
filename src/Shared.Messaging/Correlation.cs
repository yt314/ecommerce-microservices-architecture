namespace Shared.Messaging;

/// <summary>
/// The header / property name used to carry the correlation id across HTTP hops
/// (X-Correlation-ID) and, via RabbitMQ's native BasicProperties.CorrelationId,
/// across broker hops.
/// </summary>
public static class CorrelationConstants
{
    public const string HeaderName = "X-Correlation-ID";
}

/// <summary>
/// Ambient correlation id for the current logical operation, stored in an
/// AsyncLocal so it flows automatically through async/await without having to
/// thread it through every method signature.
///
/// It is populated in two places:
///   - the HTTP correlation middleware (per incoming request), and
///   - the RabbitMQ consumer base (per consumed message, from props.CorrelationId).
///
/// The <see cref="EventPublisher"/> reads it when publishing so an event always
/// carries the same id as the operation that produced it — this is what lets a
/// single order be traced across the broker, not just over HTTP headers.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
