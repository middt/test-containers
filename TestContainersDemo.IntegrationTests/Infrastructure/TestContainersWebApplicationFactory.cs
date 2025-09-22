using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Testcontainers.Redis;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Services;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Volumes;

namespace TestContainersDemo.IntegrationTests.Infrastructure;

public class TestContainersWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.0-alpine")
        .WithPortBinding(6379, true)
        .WithCommand("redis-server", "--protected-mode", "no")
        .Build();

    private readonly IContainer _mockoonContainer = new ContainerBuilder()
        .WithImage("mockoon/cli:latest")
        .WithPortBinding(3000, true)
        .WithBindMount(
            Path.Combine(Directory.GetCurrentDirectory(), "MockApi"),
            "/data")
        .WithCommand("--data", "/data/mockoon-config.json", "--port", "3000", "--hostname", "0.0.0.0")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPort(3000).ForPath("/users")))
        .Build();

    public string RedisConnectionString => _redisContainer.GetConnectionString();
    public string MockApiUrl 
    {
        get
        {
            // In containerized environments (like Kubernetes), use container IP
            var isTestingEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing";
            if (isTestingEnvironment)
            {
                return $"http://{_mockoonContainer.IpAddress}:3000";
            }
            // For local development, use mapped port
            return $"http://localhost:{_mockoonContainer.GetMappedPublicPort(3000)}";
        }
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root for containerized environments
        var isTestingEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing";
        if (isTestingEnvironment)
        {
            var containerPath = "/app/TestContainersDemo.Api";
            if (Directory.Exists(containerPath))
            {
                builder.UseContentRoot(containerPath);
            }
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection strings to use test containers
            var testConfiguration = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["ConnectionStrings:MockApi"] = MockApiUrl
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
                configuration.AllowAdmin = true; // Enable admin operations for testing
                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSingleton<IRedisService, RedisService>();

            // Remove existing UserService and HttpClient registrations
            var userServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserService));
            if (userServiceDescriptor != null)
            {
                services.Remove(userServiceDescriptor);
            }

            // Remove HttpClient factory registrations related to UserService
            var httpClientDescriptors = services.Where(d => 
                d.ServiceType.IsGenericType && 
                d.ServiceType.GetGenericTypeDefinition() == typeof(IHttpClientFactory) ||
                (d.ImplementationType?.Name.Contains("UserService") == true)).ToList();
                
            foreach (var descriptor in httpClientDescriptors)
            {
                services.Remove(descriptor);
            }

            // Re-register HttpClient with correct MockApi URL
            services.AddHttpClient<IUserService, UserService>(client =>
            {
                client.BaseAddress = new Uri(MockApiUrl);
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Start both containers in parallel for faster startup
        await Task.WhenAll(
            _redisContainer.StartAsync(),
            _mockoonContainer.StartAsync()
        );
    }

    public new async Task DisposeAsync()
    {
        // Dispose both containers in parallel
        await Task.WhenAll(
            _redisContainer.DisposeAsync().AsTask(),
            _mockoonContainer.DisposeAsync().AsTask()
        );
        await base.DisposeAsync();
    }
}
