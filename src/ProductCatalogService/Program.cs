using MongoDB.Driver;
using ProductCatalogService.Data;
using Serilog;
using Shared.Observability;
using StackExchange.Redis;

// docker-compose healthcheck entrypoint: probe /health and exit (no web host).
ObservabilityExtensions.RunHealthProbeIfRequested(args);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("ProductCatalogService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "ProductCatalogService", Version = "v1" }));

// defaultDatabase=1: NotificationService's Redis usage lives in DB 0, so the
// two don't collide on the same server.
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,defaultDatabase=1";
var redisOptions = ConfigurationOptions.Parse(redisConnection);
redisOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddScoped<ProductCache>();

var mongoConnection = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "ProductCatalog";

// IMongoClient is thread-safe by design, so it's a singleton; the IMongoDatabase
// below is a lightweight handle and can stay scoped.
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

// Reveals which replica answered — needed to prove the load balancer is
// actually spreading requests across instances.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Instance-Id"] = Environment.MachineName;
        return Task.CompletedTask;
    });
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductCatalogService v1");
    o.RoutePrefix = string.Empty; // Swagger UI at the root.
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductCatalogService" }));

app.Run();
