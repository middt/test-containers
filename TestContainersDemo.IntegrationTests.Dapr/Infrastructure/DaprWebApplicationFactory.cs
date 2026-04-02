using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;
using TestContainersDemo.Api.Interfaces;
using TestContainersDemo.Api.Services;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace TestContainersDemo.IntegrationTests.Dapr.Infrastructure;

public class DaprWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly INetwork _network = new NetworkBuilder().Build();
    private RedisContainer _redisContainer = null!;
    private IContainer _daprContainer = null!;

    private string? _redisConnectionString;
    private string? _daprHttpEndpoint;

    public string RedisConnectionString => _redisConnectionString
        ?? throw new InvalidOperationException("Factory not initialized");

    public string DaprHttpEndpoint => _daprHttpEndpoint
        ?? throw new InvalidOperationException("Factory not initialized");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["ConnectionStrings:MockApi"] = "http://localhost:9999"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace Redis
            var cmDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (cmDesc != null) services.Remove(cmDesc);

            var rsDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
            if (rsDesc != null) services.Remove(rsDesc);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var config = ConfigurationOptions.Parse(RedisConnectionString);
                config.AbortOnConnectFail = false;
                config.AllowAdmin = true;
                return ConnectionMultiplexer.Connect(config);
            });
            services.AddSingleton<IRedisService, RedisService>();

            // Replace DaprClient to point at test sidecar
            var daprDescriptors = services.Where(d =>
                d.ServiceType == typeof(DaprClient) ||
                d.ServiceType == typeof(IDaprStateService)).ToList();
            foreach (var d in daprDescriptors) services.Remove(d);

            services.AddSingleton(_ =>
                new DaprClientBuilder()
                    .UseHttpEndpoint(DaprHttpEndpoint)
                    .UseGrpcEndpoint($"http://localhost:{_daprContainer.GetMappedPublicPort(50001)}")
                    .Build());
            services.AddScoped<IDaprStateService, DaprStateService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();

        _redisContainer = new RedisBuilder("redis:7.0-alpine")
            .WithNetwork(_network)
            .WithNetworkAliases("redis")
            .WithPortBinding(6379, true)
            .WithCommand("redis-server", "--protected-mode", "no")
            .Build();

        await _redisContainer.StartAsync();
        _redisConnectionString = _redisContainer.GetConnectionString();

        var componentsDir = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents");
        var tempDir = Path.Combine(Path.GetTempPath(), $"dapr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        foreach (var file in Directory.GetFiles(componentsDir, "*.yaml"))
        {
            var content = await File.ReadAllTextAsync(file);
            content = content.Replace("REDIS_HOST", "redis");
            await File.WriteAllTextAsync(Path.Combine(tempDir, Path.GetFileName(file)), content);
        }

        _daprContainer = new ContainerBuilder("daprio/daprd:1.14.4")
            .WithNetwork(_network)
            .WithNetworkAliases("dapr")
            .WithPortBinding(3500, true)
            .WithPortBinding(50001, true)
            .WithBindMount(tempDir, "/components")
            .WithCommand(
                "./daprd",
                "--app-id", "testapp",
                "--dapr-http-port", "3500",
                "--dapr-grpc-port", "50001",
                "--resources-path", "/components",
                "--log-level", "info")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("dapr initialized"))
            .Build();

        await _daprContainer.StartAsync();
        _daprHttpEndpoint = $"http://localhost:{_daprContainer.GetMappedPublicPort(3500)}";
    }

    public new async Task DisposeAsync()
    {
        await _daprContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await _network.DisposeAsync();
        await base.DisposeAsync();
    }
}
