using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Shared.Messaging;

namespace Shared.Observability;

/// <summary>
/// First middleware in the pipeline: ensures every request has an
/// X-Correlation-ID. If the caller supplied one (e.g. the gateway forwarding to a
/// backend service) it is reused; otherwise a new one is created at this boundary.
///
/// The id is:
///   - written back onto the request headers, so YARP / outgoing HTTP clients
///     forward the SAME id downstream;
///   - echoed on the response header for the client;
///   - placed in <see cref="CorrelationContext"/> (ambient, flows to publishers); and
///   - pushed into Serilog's LogContext so every log in this request carries it.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        // Make the (possibly newly created) id available to downstream forwarders.
        context.Request.Headers[CorrelationConstants.HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        CorrelationContext.Current = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context);
    }
}

public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
