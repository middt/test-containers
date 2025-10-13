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

    // Mockoon container - initialized with inline config to work in Kubernetes
    private readonly IContainer _mockoonContainer = CreateMockoonContainer();

    public string RedisConnectionString => _redisContainer.GetConnectionString();
    private string? _cachedMockApiUrl;
    
    public string MockApiUrl 
    {
        get
        {
            if (_cachedMockApiUrl != null)
            {
                return _cachedMockApiUrl;
            }
            
            // In containerized environments (like Kubernetes), we need to get the pod IP
            var isTestingEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing";
            if (isTestingEnvironment)
            {
                // In Kubedock, query the Docker API to get the actual pod IP
                var containerId = _mockoonContainer.Id;
                try
                {
                    Console.WriteLine($"🔍 Querying Docker API for container {containerId}...");
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.BaseAddress = new Uri("http://localhost:2375");
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        var response = httpClient.GetAsync($"/containers/{containerId}/json").Result;
                        Console.WriteLine($"🔍 Docker API response status: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var json = response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine($"🔍 Docker API response (first 1000 chars): {json.Substring(0, Math.Min(1000, json.Length))}");
                            
                            // Try multiple patterns to find the IP address
                            // Pattern 1: Look for IPAddress in NetworkSettings.Networks (most common in Kubedock)
                            var ipPatterns = new[]
                            {
                                @"""IPAddress""\s*:\s*""([0-9.]+)""",                    // Standard Docker format
                                @"""Networks""[^}]*?""IPAddress""\s*:\s*""([0-9.]+)""", // Inside Networks object
                                @"""GlobalIPv4Address""\s*:\s*""([0-9.]+)""",           // Alternative field
                            };
                            
                            foreach (var pattern in ipPatterns)
                            {
                                var matches = System.Text.RegularExpressions.Regex.Matches(json, pattern);
                                Console.WriteLine($"🔍 Pattern '{pattern}' found {matches.Count} matches");
                                
                                // Try all matches to find a valid non-localhost IP
                                foreach (System.Text.RegularExpressions.Match match in matches)
                                {
                                    if (match.Success && match.Groups.Count > 1)
                                    {
                                        var ip = match.Groups[1].Value;
                                        Console.WriteLine($"🔍 Found IP candidate: {ip}");
                                        
                                        if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1" && ip != "0.0.0.0" && ip != "")
                                        {
                                            _cachedMockApiUrl = $"http://{ip}:3000";
                                            Console.WriteLine($"✅ MockApiUrl (Kubernetes mode): {_cachedMockApiUrl} (Container ID: {containerId})");
                                            return _cachedMockApiUrl;
                                        }
                                    }
                                }
                            }
                            
                            Console.WriteLine($"⚠️ No valid IP address found in Docker API response");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Docker API returned error: {response.StatusCode}");
                            var errorContent = response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine($"⚠️ Error content: {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to get real container IP from Docker API: {ex.Message}");
                    Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                }
                
                // In Kubedock with --port-forward enabled, container ports are forwarded
                // to the parent pod's localhost, but to a RANDOM port (because of WithPortBinding random assignment)
                Console.WriteLine($"🔍 Container properties - IpAddress: {_mockoonContainer.IpAddress}, Hostname: {_mockoonContainer.Hostname}, Name: {_mockoonContainer.Name}");
                Console.WriteLine($"🔍 Kubedock mode: Using localhost with port-forwarding");
                
                // Get the actual mapped port (Kubedock forwards container port to random localhost port)
                var mappedPort = _mockoonContainer.GetMappedPublicPort(3000);
                _cachedMockApiUrl = $"http://localhost:{mappedPort}";
                Console.WriteLine($"✅ MockApiUrl (Kubernetes/Kubedock mode): {_cachedMockApiUrl} (mapped port: {mappedPort})");
                return _cachedMockApiUrl;
            }
            // For local development, use mapped port
            _cachedMockApiUrl = $"http://localhost:{_mockoonContainer.GetMappedPublicPort(3000)}";
            Console.WriteLine($"🔍 MockApiUrl (Local mode): {_cachedMockApiUrl}");
            return _cachedMockApiUrl;
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
            // Note: MockApi URL will be set later via ConfigureMockApiUrl after containers start
            var testConfiguration = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["ConnectionStrings:MockApi"] = "http://placeholder:3000" // Placeholder, will be updated later
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

            // Register HttpClient with a factory that gets the URL from this class
            // The URL will be properly set after containers start
            services.AddHttpClient<IUserService, UserService>((serviceProvider, client) =>
            {
                // This lambda is called when the HttpClient is created
                // At that point, containers should already be started
                var url = MockApiUrl;
                Console.WriteLine($"🔧 Configuring UserService HttpClient with base address: {url}");
                client.BaseAddress = new Uri(url);
            });
        });
    }

    private static IContainer CreateMockoonContainer()
    {
        // Read Mockoon configuration from file
        var mockoonConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "MockApi", "mockoon-config.json");
        var mockoonConfigContent = File.ReadAllText(mockoonConfigPath);
        
        // Build Mockoon container with inline config to work in both local and Kubernetes
        return new ContainerBuilder()
            .WithImage("mockoon/cli:latest")
            .WithPortBinding(3000, true)
            .WithEnvironment("MOCKOON_CONFIG", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mockoonConfigContent)))
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand($"echo $MOCKOON_CONFIG | base64 -d > /tmp/mockoon-config.json && mockoon-cli start --data /tmp/mockoon-config.json --port 3000 --hostname 0.0.0.0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(3000))
            .Build();
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("🚀 Starting test containers...");
        Console.WriteLine($"🔍 Environment: ASPNETCORE_ENVIRONMENT={Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
        Console.WriteLine($"🔍 Current Directory: {Directory.GetCurrentDirectory()}");
        
        // Start Redis container
        Console.WriteLine("📦 Starting Redis container...");
        await _redisContainer.StartAsync();
        Console.WriteLine($"✅ Redis container started: {_redisContainer.Id}, IP: {_redisContainer.IpAddress}, Hostname: {_redisContainer.Hostname}");
        
        // Start Mockoon container
        Console.WriteLine("📦 Starting Mockoon container...");
        await _mockoonContainer.StartAsync();
        
        Console.WriteLine($"✅ Mockoon container started: {_mockoonContainer.Id}");
        Console.WriteLine($"🔍 Mockoon container details - IpAddress: {_mockoonContainer.IpAddress}, Hostname: {_mockoonContainer.Hostname}");
        
        // Now compute and cache the MockApiUrl
        var mockApiUrl = MockApiUrl; // This triggers the IP detection logic
        Console.WriteLine($"✅ MockApi URL determined: {mockApiUrl}");
        
        // Verify Mockoon is accessible with a retry mechanism
        var isTestingEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing";
        if (isTestingEnvironment)
        {
            Console.WriteLine("🔍 Verifying Mockoon accessibility via port-forwarding...");
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            
            bool isAccessible = false;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var response = await httpClient.GetAsync($"{mockApiUrl}/users");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ Mockoon is accessible at {mockApiUrl}");
                        isAccessible = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⏳ Attempt {i + 1}/10: Mockoon not yet accessible - {ex.Message}");
                    await Task.Delay(1000);
                }
            }
            
            if (!isAccessible)
            {
                Console.WriteLine($"⚠️ Warning: Mockoon may not be accessible after 10 attempts");
            }
        }
    }

    public new async Task DisposeAsync()
    {
        // Dispose Redis container
        await _redisContainer.DisposeAsync();
        
        // Dispose Mockoon container
        await _mockoonContainer.DisposeAsync();
        
        await base.DisposeAsync();
    }
}
