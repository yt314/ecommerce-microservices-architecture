using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Services;
using Serilog;
using Shared.Messaging;
using Shared.Observability;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("OrderService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "OrderService", Version = "v1" }));

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<OrderDbContext>(o => o.UseSqlServer(connectionString));

// Product validation is the one call that stays synchronous (fast-fail on a bad
// product id); the handler tags it with the correlation id so the catalog's
// logs line up with this request.
builder.Services.AddTransient<CorrelationPropagationHandler>();
builder.Services.AddHttpClient<ProductCatalogClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:ProductCatalog"] ?? "http://localhost:8081"))
    .AddHttpMessageHandler<CorrelationPropagationHandler>();

builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<OrderSagaConsumer>();

builder.Services.AddScoped<OrderProcessor>();

var app = builder.Build();

await InitializeDatabaseAsync(app);

// Correlation must be first so its id is on LogContext for every later log,
// including the Serilog request-completion log.
app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService v1");
    o.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OrderService" }));

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Order database is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
