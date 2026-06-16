using MongoDB.Driver;
using ProductCatalogService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "ProductCatalogService", Version = "v1" }));

// --- MongoDB wiring ---
// The connection string and database name come from configuration / env vars.
var mongoConnection = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "ProductCatalog";

// IMongoClient is thread-safe and meant to be a singleton.
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductCatalogService v1");
    o.RoutePrefix = string.Empty; // Swagger UI at the root.
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductCatalogService" }));

app.Run();
