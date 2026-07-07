using System.Text.Json;
using ProductCatalogService.Models;
using StackExchange.Redis;

namespace ProductCatalogService.Data;

public class ProductCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ProductRepository _repository;
    private readonly ILogger<ProductCache> _logger;

    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public ProductCache(IConnectionMultiplexer redis, ProductRepository repository, ILogger<ProductCache> logger)
    {
        _redis = redis;
        _repository = repository;
        _logger = logger;
    }

    private static string Key(string id) => $"catalog:product:{id}";

    public async Task<Product?> GetByIdAsync(string id)
    {
        var db = _redis.GetDatabase();
        var key = Key(id);

        var cached = await db.StringGetAsync(key);
        if (!cached.IsNullOrEmpty)
        {
            _logger.LogInformation("CACHE HIT  {Key}", key);
            return JsonSerializer.Deserialize<Product>(cached!);
        }

        _logger.LogInformation("CACHE MISS {Key} — reading MongoDB", key);
        var product = await _repository.GetByIdAsync(id);
        if (product is not null)
            await db.StringSetAsync(key, JsonSerializer.Serialize(product), Ttl);

        return product;
    }

    public async Task InvalidateAsync(string id)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(id));
        _logger.LogInformation("CACHE INVALIDATE {Key}", Key(id));
    }
}
