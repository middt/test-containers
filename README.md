# Testcontainers Integration Demo

.NET 10 Web API with Redis caching and Users API, fully tested using **Testcontainers** and **xUnit**. Integration tests automatically spin up Redis and Mockoon containers.

## Project Structure

```
TestContainersDemo.Api/                  # Web API (Redis + Users endpoints)
TestContainersDemo.UnitTests/            # 29 unit tests (Moq-based, no containers)
TestContainersDemo.IntegrationTests/     # 38 integration tests (Testcontainers)
k8s/                                     # Kubernetes manifests & scripts
```

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop

## Quick Start

```bash
dotnet restore
dotnet build
dotnet test        # runs all 67 tests
```

Run the API locally:

```bash
docker run --name redis-demo -p 6379:6379 -d redis:7.0-alpine
dotnet run --project TestContainersDemo.Api
```

## CI/CD

A GitHub Actions workflow (`.github/workflows/integration-tests.yml`) runs on PRs targeting `main` or `release-*` branches. It executes two parallel jobs:

| Job | What it runs | Docker required |
|-----|-------------|-----------------|
| **Unit Tests** | `TestContainersDemo.UnitTests` (29 tests) | No |
| **Integration Tests** | `TestContainersDemo.IntegrationTests` (38 tests) | Yes |

Both jobs report results line-by-line in the PR checks tab via [dorny/test-reporter](https://github.com/dorny/test-reporter). Pushes to the same PR cancel in-progress runs.

### Require tests before merge

1. **Settings > Branches > Add rule** for `main` (repeat for `release-*`)
2. Enable **Require status checks to pass before merging**
3. Select **Unit Tests** and **Integration Tests**

## Test Coverage

| Project | Tests | Scope |
|---------|-------|-------|
| **UnitTests / Controllers** | 19 | CacheController, UsersController (mocked services) |
| **UnitTests / Services** | 7 | UserService (mocked HttpClient) |
| **UnitTests / Models** | 3 | User, CreateUserRequest defaults |
| **IntegrationTests / Cache** | 20 | CacheController + RedisService against real Redis |
| **IntegrationTests / Users** | 17 | UsersController + UserService against real Mockoon |
| **IntegrationTests / Other** | 1 | Endpoint routing |
| **Total** | **67** | |

## Kubernetes Deployment

> Kubedock runs as a sidecar — no separate installation needed.
> Mockoon tests that require file mounting will fail in K8s (23/38 pass). Run locally for full coverage.

```bash
./k8s/scripts/deploy.sh              # deploy
./k8s/scripts/test-deployment.sh     # check results
kubectl delete namespace testcontainers-demo  # cleanup
```

## Troubleshooting

**Kubedock stuck on leader lease:**

```bash
kubectl delete lease kubedock-lock -n testcontainers-demo
kubectl delete job testcontainers-demo-job-image -n testcontainers-demo
kubectl apply -f k8s/manifests/05-test-job-with-image.yaml
```

**Check logs:**

```bash
kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c kubedock -f
kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c tests -f
```

## Dependencies

| Package | Purpose |
|---------|---------|
| `StackExchange.Redis` | Redis client |
| `Testcontainers.Redis` | Redis container for tests |
| `DotNet.Testcontainers` | Generic container support |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration test host |
| `Moq` | Mocking for unit tests |
| `xUnit` | Test framework |
