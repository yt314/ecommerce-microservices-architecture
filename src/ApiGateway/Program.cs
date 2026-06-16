using Serilog;
using Shared.Observability;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

// Phase 5: structured logging to console + Seq.
builder.AddObservability("ApiGateway");

// YARP reverse proxy loaded entirely from the "ReverseProxy" config section.
// The gateway has NO domain logic — it only routes traffic to internal services.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Create/accept the correlation id at the system boundary and write it onto the
// request headers so YARP forwards the SAME id to every downstream service.
app.UseCorrelationId();
app.UseSerilogRequestLogging();

// Gateway's own health endpoint (not proxied).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

// Everything else is matched by the configured routes and forwarded.
app.MapReverseProxy();

app.Run();
