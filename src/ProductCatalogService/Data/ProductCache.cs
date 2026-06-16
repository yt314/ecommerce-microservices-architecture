using System.Text.Json;
using ProductCatalogService.Models;
using StackExchange.Redis;

namespace ProductCatalogService.Data;

/// <summary>
/// Cache-aside layer over <see cref="ProductRepository"/> for single-product reads.
///
///   read  : try Redis → HIT returns cached; MISS reads MongoDB then populates Redis.
///   update: caller invalidates (deletes) the key so the next read repopulates.
///
/// Keys are "catalog:product:{id}" and live in Redis logical DB 1, completely
/// separate from NotificationService (which uses DB 0).
/// </summary>
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

    /// <summary>Removes the cached product so the next read fetches fresh data.</summary>
    public async Task InvalidateAsync(string id)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(id));
        _logger.LogInformation("CACHE INVALIDATE {Key}", Key(id));
    }
}
