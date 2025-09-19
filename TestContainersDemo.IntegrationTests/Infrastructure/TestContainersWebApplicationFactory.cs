using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Services;

namespace TestContainersDemo.IntegrationTests.Infrastructure;

public class TestContainersWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.0-alpine")
        .WithPortBinding(6379, true)
        .Build();

    public string RedisConnectionString => _redisContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override the Redis connection string to use the test container
            var testConfiguration = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = RedisConnectionString
            };

            config.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing Redis connection registration
            var connectionMultiplexerDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (connectionMultiplexerDescriptor != null)
            {
                services.Remove(connectionMultiplexerDescriptor);
            }

            var redisServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IRedisService));
            if (redisServiceDescriptor != null)
            {
                services.Remove(redisServiceDescriptor);
            }

            // Add new Redis connection using test container
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse(RedisConnectionString);
                configuration.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddScoped<IRedisService, RedisService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
