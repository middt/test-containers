# .NET Core Testcontainers Integration Demo

This project demonstrates **Testcontainers** integration testing in a .NET Core application with **Redis caching** and **Users API** functionality. Tests automatically spin up Redis and Mockoon containers for complete integration testing.

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
- **Comprehensive Testing**: 37 integration tests (100% pass rate)

## Prerequisites

- **.NET 9.0 SDK**
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

### GitHub Actions Example
```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Run integration tests
      run: dotnet test --no-build --verbosity normal
```

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
    version: '9.0.x'

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

### 2. Install Kubedock (Required for Testcontainers in K8s)
```bash
# Install Kubedock in your cluster
kubectl apply -f https://raw.githubusercontent.com/joyrex2001/kubedock/main/deploy/kubedock.yaml
```

### 3. Deploy and Run Tests
```bash
# Deploy to Kubernetes
./k8s/scripts/deploy.sh

# Check test results
./k8s/scripts/test-deployment.sh

# Expected output:
# 🎉 All tests passed! The Kubernetes deployment is working correctly.
# Test Run Successful.
# Total tests: 37
#      Passed: 37
```

### 4. Cleanup
```bash
# Delete the deployment
kubectl delete namespace testcontainers-demo
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