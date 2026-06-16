using MongoDB.Driver;
using ProductCatalogService.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "ProductCatalogService", Version = "v1" }));

// --- Redis cache-aside wiring (logical DB 1 — separate from NotificationService) ---
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,defaultDatabase=1";
var redisOptions = ConfigurationOptions.Parse(redisConnection);
redisOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddScoped<ProductCache>();

// --- MongoDB wiring ---
// The connection string and database name come from configuration / env vars.
var mongoConnection = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "ProductCatalog";

// IMongoClient is thread-safe and meant to be a singleton.
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();

// Stamp every response with the container/host that served it. With multiple
// replicas behind the load balancer, this header reveals which instance answered.
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
