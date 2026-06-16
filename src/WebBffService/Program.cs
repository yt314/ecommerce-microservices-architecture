using Serilog;
using Shared.Observability;
using WebBffService.Clients;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

// Phase 5: structured logging to console + Seq.
builder.AddObservability("WebBffService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "WebBffService", Version = "v1" }));

// Typed HTTP clients to the backend services. Base addresses come from config
// (env vars in docker-compose). The catalog client points at the load balancer.
// The propagation handler carries the correlation id onto both aggregated calls.
builder.Services.AddTransient<CorrelationPropagationHandler>();
builder.Services.AddHttpClient<OrderClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Order"] ?? "http://localhost:8083"))
    .AddHttpMessageHandler<CorrelationPropagationHandler>();
builder.Services.AddHttpClient<ProductCatalogClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:ProductCatalog"] ?? "http://localhost:8081"))
    .AddHttpMessageHandler<CorrelationPropagationHandler>();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "WebBffService v1");
    o.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "WebBffService" }));

app.Run();
