using TestContainersDemo.IntegrationTests.Infrastructure;

namespace TestContainersDemo.IntegrationTests.Tests;

public class RedisServiceTests : IntegrationTestBase
{
    public RedisServiceTests(TestContainersWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SetAsync_ShouldReturnTrue_WhenSettingValue()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-test-key";
        const string value = "service-test-value";

        // Act
        var result = await RedisService.SetAsync(key, value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnValue_WhenKeyExists()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-get-key";
        const string value = "service-get-value";
        
        await RedisService.SetAsync(key, value);

        // Act
        var result = await RedisService.GetAsync(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-service-key";

        // Act
        var result = await RedisService.GetAsync(nonExistentKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-delete-key";
        const string value = "service-delete-value";
        
        await RedisService.SetAsync(key, value);

        // Act
        var result = await RedisService.DeleteAsync(key);

        // Assert
        Assert.True(result);
        
        // Verify key no longer exists
        var getValue = await RedisService.GetAsync(key);
        Assert.Null(getValue);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-delete-key";

        // Act
        var result = await RedisService.DeleteAsync(nonExistentKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-exists-key";
        const string value = "service-exists-value";
        
        await RedisService.SetAsync(key, value);

        // Act
        var result = await RedisService.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-exists-key";

        // Act
        var result = await RedisService.ExistsAsync(nonExistentKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IncrementAsync_ShouldReturnIncrementedValue()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-increment-key";

        // Act - First increment should return 1
        var firstResult = await RedisService.IncrementAsync(key);
        
        // Assert
        Assert.Equal(1, firstResult);

        // Act - Second increment should return 2
        var secondResult = await RedisService.IncrementAsync(key);
        
        // Assert
        Assert.Equal(2, secondResult);
    }

    [Fact]
    public async Task DecrementAsync_ShouldReturnDecrementedValue()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-decrement-key";
        
        // Set initial value
        await RedisService.SetAsync(key, "10");

        // Act
        var result = await RedisService.DecrementAsync(key);

        // Assert
        Assert.Equal(9, result);
    }

    [Fact]
    public async Task SetAsync_WithExpiry_ShouldSetExpiringKey()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "service-expiry-key";
        const string value = "service-expiry-value";
        var expiry = TimeSpan.FromSeconds(30); // 30 seconds for testing

        // Act
        var result = await RedisService.SetAsync(key, value, expiry);

        // Assert
        Assert.True(result);
        
        // Verify key exists
        var exists = await RedisService.ExistsAsync(key);
        Assert.True(exists);
        
        // Verify value is correct
        var retrievedValue = await RedisService.GetAsync(key);
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public async Task SetAsync_MultipleKeys_ShouldHandleConcurrentOperations()
    {
        // Arrange
        await CleanupRedisAsync();
        const int keyCount = 10;
        var tasks = new List<Task>();

        // Act - Set multiple keys concurrently
        for (int i = 0; i < keyCount; i++)
        {
            var keyIndex = i;
            tasks.Add(RedisService.SetAsync($"concurrent-key-{keyIndex}", $"concurrent-value-{keyIndex}"));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all keys were set
        for (int i = 0; i < keyCount; i++)
        {
            var value = await RedisService.GetAsync($"concurrent-key-{i}");
            Assert.Equal($"concurrent-value-{i}", value);
        }
    }
}
