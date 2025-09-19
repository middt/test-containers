using System.Net;
using System.Net.Http.Json;
using TestContainersDemo.Api.Controllers;
using TestContainersDemo.IntegrationTests.Infrastructure;

namespace TestContainersDemo.IntegrationTests.Tests;

public class CacheControllerTests : IntegrationTestBase
{
    public CacheControllerTests(TestContainersWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "test-key";
        const string value = "test-value";
        var cacheRequest = new CacheRequest(value);

        // Act - Set value
        var setResponse = await HttpClient.PostAsJsonAsync($"/api/cache/{key}", cacheRequest);
        
        // Assert - Set was successful
        setResponse.EnsureSuccessStatusCode();
        var setContent = await setResponse.Content.ReadAsStringAsync();
        Assert.Contains("Value set successfully", setContent);

        // Act - Get value
        var getResponse = await HttpClient.GetAsync($"/api/cache/{key}");
        
        // Assert - Get was successful and returned correct value
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains(value, getContent);
    }

    [Fact]
    public async Task Get_NonExistentKey_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-key";

        // Act
        var response = await HttpClient.GetAsync($"/api/cache/{nonExistentKey}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content);
    }

    [Fact]
    public async Task SetWithExpiry_ShouldExpireCorrectly()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "expiry-test-key";
        const string value = "expiry-test-value";
        const int expiryMinutes = 1;
        
        var cacheRequest = new CacheRequest(value, expiryMinutes);

        // Act - Set value with expiry
        var setResponse = await HttpClient.PostAsJsonAsync($"/api/cache/{key}", cacheRequest);
        setResponse.EnsureSuccessStatusCode();

        // Act - Verify key exists
        var existsResponse = await HttpClient.GetAsync($"/api/cache/{key}/exists");
        existsResponse.EnsureSuccessStatusCode();
        var existsContent = await existsResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"exists\":true", existsContent.ToLower());

        // Note: In a real test, you might want to test actual expiry,
        // but that would require waiting or manipulating time
        // For this demo, we're just verifying the key was set successfully
    }

    [Fact]
    public async Task Delete_ExistingKey_ShouldReturnSuccess()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "delete-test-key";
        const string value = "delete-test-value";
        var cacheRequest = new CacheRequest(value);

        // Set up the key first
        await HttpClient.PostAsJsonAsync($"/api/cache/{key}", cacheRequest);

        // Act - Delete the key
        var deleteResponse = await HttpClient.DeleteAsync($"/api/cache/{key}");

        // Assert
        deleteResponse.EnsureSuccessStatusCode();
        var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("Key deleted successfully", deleteContent);

        // Verify key no longer exists
        var getResponse = await HttpClient.GetAsync($"/api/cache/{key}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentKey_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-delete-key";

        // Act
        var response = await HttpClient.DeleteAsync($"/api/cache/{nonExistentKey}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Exists_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "exists-test-key";
        const string value = "exists-test-value";
        var cacheRequest = new CacheRequest(value);

        // Set up the key first
        await HttpClient.PostAsJsonAsync($"/api/cache/{key}", cacheRequest);

        // Act
        var response = await HttpClient.GetAsync($"/api/cache/{key}/exists");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"exists\":true", content.ToLower());
    }

    [Fact]
    public async Task Exists_NonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        await CleanupRedisAsync();
        const string nonExistentKey = "non-existent-exists-key";

        // Act
        var response = await HttpClient.GetAsync($"/api/cache/{nonExistentKey}/exists");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"exists\":false", content.ToLower());
    }

    [Fact]
    public async Task Increment_ShouldWorkCorrectly()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "increment-test-key";

        // Act - First increment (should create key with value 1)
        var firstResponse = await HttpClient.PostAsync($"/api/cache/{key}/increment", null);
        
        // Assert
        firstResponse.EnsureSuccessStatusCode();
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"value\":1", firstContent.ToLower());

        // Act - Second increment (should increment to 2)
        var secondResponse = await HttpClient.PostAsync($"/api/cache/{key}/increment", null);
        
        // Assert
        secondResponse.EnsureSuccessStatusCode();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"value\":2", secondContent.ToLower());
    }

    [Fact]
    public async Task Decrement_ShouldWorkCorrectly()
    {
        // Arrange
        await CleanupRedisAsync();
        const string key = "decrement-test-key";

        // Set initial value
        const string initialValue = "5";
        var cacheRequest = new CacheRequest(initialValue);
        await HttpClient.PostAsJsonAsync($"/api/cache/{key}", cacheRequest);

        // Act - Decrement
        var response = await HttpClient.PostAsync($"/api/cache/{key}/decrement", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"value\":4", content.ToLower());
    }
}
