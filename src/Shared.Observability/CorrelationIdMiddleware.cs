using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Shared.Messaging;

namespace Shared.Observability;

// Reuses the caller's X-Correlation-ID if present, otherwise creates one here.
// Written back onto the request (so YARP forwards it downstream), echoed on
// the response, and pushed into both CorrelationContext and Serilog's
// LogContext so everything downstream — logs, outgoing calls, published
// events — ends up tagged with the same id.
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

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
