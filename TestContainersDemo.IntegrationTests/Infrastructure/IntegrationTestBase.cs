using Microsoft.Extensions.DependencyInjection;
using TestContainersDemo.Api.Interfaces;

namespace TestContainersDemo.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<TestContainersWebApplicationFactory>
{
    protected readonly TestContainersWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;

    protected IntegrationTestBase(TestContainersWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
    }

    protected async Task CleanupRedisAsync()
    {
        // Helper method to clean up Redis data between tests
        // This is a simple implementation - in production, you might want to flush specific keys
        var connectionMultiplexer = Factory.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }

    protected IRedisService GetRedisService()
    {
        // Get singleton service directly from root provider
        return Factory.Services.GetRequiredService<IRedisService>();
    }
}
