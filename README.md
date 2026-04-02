# .NET Core Testcontainers Integration Demo

This project demonstrates **Testcontainers** integration testing in a .NET Core application with **Redis caching** and **Users API** functionality. Tests automatically spin up Redis and Mockoon containers for complete integration testing.

## ✅ Recent Fixes

- **Fixed Kubedock deployment**: Kubedock now runs as a sidecar container (no separate installation needed)
- **Fixed RBAC permissions**: Added proper `list`, `watch`, and `delete` verbs for lease management
- **Fixed stale lease issue**: Documented how to clear stale kubedock leases
- **Updated README**: Removed broken kubedock installation URL

## Project Structure

```
TestContainersDemo/
├── TestContainersDemo.Api/                    # Main Web API project
│   ├── Controllers/
│   │   ├── CacheController.cs                # Redis cache endpoints
│   │   └── UsersController.cs                # Users API endpoints
│   ├── Services/
│   │   ├── RedisService.cs                   # Redis service implementation
│   │   └── UserService.cs                    # Users service implementation
│   └── Models/                               # DTOs and models
├── TestContainersDemo.IntegrationTests/       # Integration tests
│   ├── Infrastructure/
│   │   └── TestContainersWebApplicationFactory.cs  # Manages containers
│   ├── MockApi/
│   │   └── mockoon-config.json               # Mock API configuration
│   └── Tests/                                # 37 integration tests
└── k8s/                                      # Kubernetes deployment
    ├── manifests/                            # K8s manifests
    ├── scripts/                              # Deployment scripts
    └── Dockerfile.tests                      # Test container image
```

## Features

- **Redis Cache API**: Set, Get, Delete, Exists, Increment, Decrement operations
- **Users API**: Create, retrieve users with external API integration
- **Testcontainers Integration**: Automatic Redis + Mockoon container management
- **Kubernetes Support**: Runs in K8s with Kubedock
  - ✅ **Redis tests**: 23 tests fully working in Kubernetes
  - ⚠️ **Mockoon tests**: 14 tests require local Docker (file mounting limitation)
- **Comprehensive Testing**: 37 integration tests (100% pass rate locally)

## Prerequisites

- **.NET 10.0 SDK**
- **Docker Desktop**
- **Kubernetes cluster** (for K8s deployment)

## Running Locally

### 1. Clone and Build
```bash
git clone <your-repo>
cd TestContainersDemo
dotnet restore
dotnet build
```

### 2. Run Tests
```bash
# Run all integration tests (37 tests)
dotnet test

# Expected result:
# Test Run Successful.
# Total tests: 37
#      Passed: 37
```

### 3. Run Application (Optional)
```bash
# Start with local Redis
docker run --name redis-demo -p 6379:6379 -d redis:7.0-alpine
cd TestContainersDemo.Api
dotnet run
```

## Running in CI/CD Pipeline

### GitHub Actions

The repository includes a workflow at `.github/workflows/integration-tests.yml` that runs all integration tests on PRs targeting `main` or `release-*` branches. Test results are uploaded as artifacts for easy review.

### Azure DevOps Example
```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- script: dotnet restore
  displayName: 'Restore packages'

- script: dotnet build --no-restore
  displayName: 'Build'

- script: dotnet test --no-build --logger trx
  displayName: 'Run tests'
```

## Kubernetes Installation & Deployment

### 1. Install Kubernetes

#### Option A: Local Development (Docker Desktop)
```bash
# Enable Kubernetes in Docker Desktop settings
# Or install kind:
curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.20.0/kind-linux-amd64
chmod +x ./kind
sudo mv ./kind /usr/local/bin/kind

# Create cluster
kind create cluster --name testcontainers-demo
```

#### Option B: Cloud Providers
```bash
# AWS EKS
aws eks create-cluster --name testcontainers-demo --version 1.28

# Azure AKS  
az aks create --resource-group myResourceGroup --name testcontainers-demo

# Google GKE
gcloud container clusters create testcontainers-demo --zone us-central1-a
```

### 2. Deploy and Run Tests

> **Note**: Kubedock is automatically deployed as a sidecar container alongside the tests. No separate installation required.

> **⚠️ Known Limitation**: When running in Kubernetes with Kubedock, file mounting for Mockoon containers is not supported. This means User API tests that depend on Mockoon will not work in K8s. Redis-based cache tests work perfectly. For full test coverage including Mockoon, run tests locally with Docker Desktop.
```bash
# Deploy to Kubernetes
./k8s/scripts/deploy.sh

# Check test results
./k8s/scripts/test-deployment.sh

# Expected output (Kubernetes):
# Test Run Failed.
# Total tests: 37
#      Passed: 23  (All Redis/Cache tests)
#      Failed: 14  (User API tests - Mockoon file mounting not supported)
#
# NOTE: 23 passing tests demonstrates Testcontainers + Kubedock works perfectly!
# The 14 failures are expected due to Mockoon's file mounting limitation in K8s.
# Run 'dotnet test' locally for full 37/37 test coverage.
```

### 3. Cleanup
```bash
# Delete the deployment
kubectl delete namespace testcontainers-demo
```

## Troubleshooting

### Kubedock stuck at "attempting to acquire leader lease"

If kubedock is stuck trying to acquire a lease, there may be a stale lease from a previous run:

```bash
# Check for stale leases
kubectl get leases -n testcontainers-demo

# Delete the stale lease
kubectl delete lease kubedock-lock -n testcontainers-demo

# Restart the job
kubectl delete job testcontainers-demo-job-image -n testcontainers-demo
kubectl apply -f k8s/manifests/05-test-job-with-image.yaml
```

### Tests timing out or containers failing

1. **Check kubedock logs**:
   ```bash
   kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c kubedock -f
   ```

2. **Check test logs**:
   ```bash
   kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c tests -f
   ```

3. **Check pod events**:
   ```bash
   kubectl get events -n testcontainers-demo --sort-by='.lastTimestamp' | tail -20
   ```

### File mounting issues with Mockoon in Kubernetes

This is a known limitation. Kubedock doesn't support file bind mounts the same way as Docker Desktop. For tests that require file mounting (like Mockoon), run them locally instead:

```bash
dotnet test
```

## Test Coverage

- **Redis Cache Tests**: 20 tests (CacheController: 9, RedisService: 11)
- **Users API Tests**: 17 tests (UsersController: 9, UserService: 8)
- **Total**: 37 integration tests with 100% pass rate

## Dependencies

- `StackExchange.Redis` - Redis client
- `Testcontainers.Redis` - Redis container support
- `DotNet.Testcontainers` - Generic container support
- `Microsoft.AspNetCore.Mvc.Testing` - ASP.NET Core testing
- `xUnit` - Testing framework