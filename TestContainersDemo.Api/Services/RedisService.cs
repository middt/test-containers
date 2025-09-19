using StackExchange.Redis;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.Api.Services;

public class RedisService : IRedisService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
    }

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _database.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task<long> IncrementAsync(string key)
    {
        return await _database.StringIncrementAsync(key);
    }

    public async Task<long> DecrementAsync(string key)
    {
        return await _database.StringDecrementAsync(key);
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
