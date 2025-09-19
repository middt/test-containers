# .NET Core Testcontainers Redis Demo

This project demonstrates how to use **Testcontainers** for integration testing with Redis in a .NET Core application. The solution includes a Web API that interacts with Redis and comprehensive integration tests that spin up a Redis container automatically.

## Project Structure

```
TestContainersDemo/
├── TestContainersDemo.sln
├── TestContainersDemo.Api/                    # Main Web API project
│   ├── Controllers/CacheController.cs         # Redis cache endpoints
│   ├── Interfaces/IRedisService.cs            # Redis service interface
│   ├── Services/RedisService.cs               # Redis service implementation
│   └── Program.cs                             # Application configuration
└── TestContainersDemo.IntegrationTests/       # Integration tests project
    ├── Infrastructure/                        # Test infrastructure
    │   ├── TestContainersWebApplicationFactory.cs
    │   └── IntegrationTestBase.cs
    └── Tests/                                 # Test files
        ├── CacheControllerTests.cs
        └── RedisServiceTests.cs
```

## Features

### Web API Features
- **Redis Cache Operations**: Set, Get, Delete, Exists, Increment, Decrement
- **TTL Support**: Set cache entries with expiration times
- **RESTful API**: Clean REST endpoints for cache operations
- **Error Handling**: Comprehensive error handling and logging
- **Dependency Injection**: Proper DI setup for Redis services

### Testing Features
- **Testcontainers Integration**: Automatic Redis container management
- **Integration Tests**: Tests that verify actual Redis interactions
- **Test Isolation**: Each test runs against a clean Redis instance
- **Concurrent Testing**: Tests for concurrent Redis operations

## Prerequisites

- **.NET 9.0 SDK** or later
- **Docker Desktop** (required for Testcontainers)

## Getting Started

### 1. Clone and Build

```bash
git clone <your-repo-url>
cd TestContainersDemo
dotnet restore
dotnet build
```

### 2. Running the Application

#### Option A: With Local Redis
If you have Redis running locally on port 6379:

```bash
cd TestContainersDemo.Api
dotnet run
```

#### Option B: With Docker Redis
Start a Redis container:

```bash
docker run --name redis-demo -p 6379:6379 -d redis:7.0-alpine
cd TestContainersDemo.Api
dotnet run
```

### 3. Testing the API

Once the application is running (default: https://localhost:5001), you can test the endpoints:

#### Set a value:
```bash
curl -X POST https://localhost:5001/api/cache/mykey \
  -H "Content-Type: application/json" \
  -d '{"value": "Hello World"}'
```

#### Get a value:
```bash
curl https://localhost:5001/api/cache/mykey
```

#### Check if key exists:
```bash
curl https://localhost:5001/api/cache/mykey/exists
```

#### Increment a counter:
```bash
curl -X POST https://localhost:5001/api/cache/counter/increment
```

#### Delete a key:
```bash
curl -X DELETE https://localhost:5001/api/cache/mykey
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/cache/{key}` | Set a cache value with optional expiry |
| GET | `/api/cache/{key}` | Get a cache value |
| DELETE | `/api/cache/{key}` | Delete a cache key |
| GET | `/api/cache/{key}/exists` | Check if key exists |
| POST | `/api/cache/{key}/increment` | Increment a numeric value |
| POST | `/api/cache/{key}/decrement` | Decrement a numeric value |

### Request/Response Examples

#### Set Value Request:
```json
{
  "value": "Hello World",
  "expiryMinutes": 60
}
```

#### Get Value Response:
```json
{
  "key": "mykey",
  "value": "Hello World"
}
```

## Running Integration Tests

### Local Development

The integration tests use **Testcontainers** to automatically spin up a Redis container:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run only integration tests
dotnet test TestContainersDemo.IntegrationTests

# Run a specific test class
dotnet test --filter "FullyQualifiedName~CacheControllerTests"
```

### Kubernetes Deployment with Kubedock

For running tests in Kubernetes environments, this project includes [Kubedock](https://github.com/joyrex2001/kubedock) integration:

```bash
# Deploy to local KIND cluster (recommended for development)
./k8s/scripts/deploy-kind.sh

# Or deploy to existing Kubernetes cluster
./k8s/scripts/deploy.sh
```

**Kubedock Benefits:**
- ✅ No Docker-in-Docker required
- ✅ Test containers run as Kubernetes pods  
- ✅ Better resource management and security
- ✅ Perfect for CI/CD pipelines (Tekton, GitHub Actions, etc.)

See [k8s/README.md](k8s/README.md) for detailed Kubernetes deployment instructions.

### What the Tests Cover

#### Controller Tests (`CacheControllerTests`)
- ✅ Set and get operations
- ✅ Non-existent key handling
- ✅ Cache expiry functionality
- ✅ Delete operations
- ✅ Key existence checks
- ✅ Increment/decrement operations

#### Service Tests (`RedisServiceTests`)
- ✅ Direct Redis service operations
- ✅ Concurrent operations
- ✅ TTL (Time To Live) functionality
- ✅ Error handling scenarios

## How Testcontainers Works

### 1. Test Container Setup
The `TestContainersWebApplicationFactory` automatically:
- Starts a Redis container before tests run
- Configures the application to use the test container
- Provides a clean Redis instance for each test class

### 2. Container Lifecycle
```csharp
// Container is started once per test class
public async Task InitializeAsync()
{
    await _redisContainer.StartAsync();
}

// Container is disposed after all tests complete
public async Task DisposeAsync()
{
    await _redisContainer.DisposeAsync();
}
```

### 3. Test Isolation
Each test method calls `CleanupRedisAsync()` to ensure a clean state:
```csharp
protected async Task CleanupRedisAsync()
{
    var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
    await server.FlushDatabaseAsync();
}
```

## Configuration

### Application Configuration
The Redis connection string can be configured in:

- `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

- Environment variables:
```bash
export ConnectionStrings__Redis="localhost:6379"
```

### Test Configuration
Tests automatically configure Redis to use the Testcontainer:
```csharp
var testConfiguration = new Dictionary<string, string?>
{
    ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString()
};
```

## Benefits of This Approach

### 1. **Real Integration Testing**
- Tests run against actual Redis instances
- No mocking of Redis behavior
- Catches Redis-specific issues

### 2. **Test Isolation**
- Each test class gets its own container
- Tests don't interfere with each other
- Consistent test environment

### 3. **CI/CD Friendly**
- No external dependencies in CI
- Containers are managed automatically
- Works on any system with Docker

### 4. **Development Productivity**
- No need to manually start/stop Redis
- Tests run the same way locally and in CI
- Easy debugging with real Redis data

## Troubleshooting

### Docker Issues
If tests fail with Docker-related errors:

1. **Check Docker is running**:
```bash
docker info
```

2. **Check Docker permissions** (Linux/Mac):
```bash
sudo usermod -aG docker $USER
# Log out and back in
```

3. **Clean up containers**:
```bash
docker system prune -f
```

### Redis Connection Issues
If you see Redis connection errors:

1. **Check Redis container logs**:
```bash
docker logs <container-id>
```

2. **Verify port binding**:
```bash
docker ps
```

3. **Test Redis connectivity**:
```bash
docker exec -it <container-id> redis-cli ping
```

### Test Issues
If tests are flaky:

1. **Increase timeout values** in test setup
2. **Check for port conflicts** with other services
3. **Ensure proper test cleanup** between test runs

## Next Steps

To extend this project, consider:

1. **Adding more Redis features** (pub/sub, streams, etc.)
2. **Implementing caching patterns** (cache-aside, write-through)
3. **Adding metrics and monitoring**
4. **Performance testing** with Testcontainers
5. **Testing Redis cluster configurations**

## Dependencies

### Main Project
- `StackExchange.Redis` - Redis client library
- `Microsoft.AspNetCore` - Web API framework

### Test Project
- `Testcontainers.Redis` - Redis Testcontainers support
- `Microsoft.AspNetCore.Mvc.Testing` - ASP.NET Core testing
- `xUnit` - Testing framework

## License

This project is provided as a demonstration and learning resource.
