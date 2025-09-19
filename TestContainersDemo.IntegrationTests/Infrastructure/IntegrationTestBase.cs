using Microsoft.Extensions.DependencyInjection;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<TestContainersWebApplicationFactory>
{
    protected readonly TestContainersWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;
    protected readonly IRedisService RedisService;

    protected IntegrationTestBase(TestContainersWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        RedisService = factory.Services.GetRequiredService<IRedisService>();
    }

    protected async Task CleanupRedisAsync()
    {
        // Helper method to clean up Redis data between tests
        // This is a simple implementation - in production, you might want to flush specific keys
        var connectionMultiplexer = Factory.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }
}
