using ECommerce.Monolith.Api.Data;
using ECommerce.Monolith.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "ECommerce Monolith API", Version = "v1" });
});

// ConnectionStrings__Default is the Docker env var form of this same key.
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// Retries because SQL Server, in a sibling container, can take a while to
// start accepting connections.
await InitializeDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce Monolith API v1");
    options.RoutePrefix = string.Empty;
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Database is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{Max}). Retrying in 5s...",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
