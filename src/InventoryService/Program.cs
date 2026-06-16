using InventoryService.Data;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Messaging;
using Shared.Observability;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

// Phase 5: structured logging to console + Seq.
builder.AddObservability("InventoryService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "InventoryService", Version = "v1" }));

// EF Core + PostgreSQL. Connection string comes from config / env var.
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<InventoryManager>();

// RabbitMQ: connection + publisher, plus the saga consumer (OrderPlaced).
builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<InventorySagaConsumer>();

var app = builder.Build();

// Create the schema on startup, retrying while PostgreSQL finishes booting.
await InitializeDatabaseAsync(app);

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryService v1");
    o.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "InventoryService" }));

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Inventory database is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
