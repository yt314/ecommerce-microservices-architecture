namespace Shared.Messaging;

public static class CorrelationConstants
{
    public const string HeaderName = "X-Correlation-ID";
}

// Stored in an AsyncLocal so the id flows through async/await automatically,
// without threading it through every method signature. Set from two places —
// the HTTP middleware on incoming requests, and the RabbitMQ consumer base on
// consumed messages — and read by EventPublisher on the way out, which is what
// lets one order be traced across the broker, not just over HTTP headers.
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
