using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "OrderService", Version = "v1" }));

// EF Core + SQL Server (this service's own database).
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<OrderDbContext>(o => o.UseSqlServer(connectionString));

// Typed HTTP clients for the other services. Base addresses come from config
// (env vars in docker-compose), so OrderService never reaches another database.
builder.Services.AddHttpClient<ProductCatalogClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:ProductCatalog"] ?? "http://localhost:8081"));
builder.Services.AddHttpClient<InventoryClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Inventory"] ?? "http://localhost:8082"));
builder.Services.AddHttpClient<NotificationClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Notification"] ?? "http://localhost:8084"));

builder.Services.AddScoped<OrderProcessor>();

var app = builder.Build();

// Create the schema on startup, retrying while SQL Server finishes booting.
await InitializeDatabaseAsync(app);

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
