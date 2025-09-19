namespace TestContainersDemo.Api.Interfaces;

public interface IRedisService
{
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key);
    Task<long> DecrementAsync(string key);
}
