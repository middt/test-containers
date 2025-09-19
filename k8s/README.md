# Running Testcontainers Demo on Kubernetes with Kubedock

This directory contains the necessary configurations to run the Testcontainers Demo on Kubernetes using [Kubedock](https://github.com/joyrex2001/kubedock), which provides a Docker API that orchestrates containers on Kubernetes instead of running them locally.

## 📋 Prerequisites

- **Kubernetes cluster** (local KIND, minikube, or cloud-based)
- **kubectl** configured to access your cluster
- **Docker** for building images
- **kind** (optional, for local development)

## 🏗️ Architecture

The solution consists of:

1. **Test Job Pod** with two containers:
   - **Tests Container**: Runs the .NET integration tests
   - **Kubedock Sidecar**: Provides Docker API that creates test containers as Kubernetes pods

2. **RBAC Resources**: ServiceAccount, Role, and RoleBinding for Kubedock permissions

3. **ConfigMap**: Contains environment variables for Testcontainers and Kubedock configuration

## 🚀 Quick Start

### Option 1: Deploy to Local KIND Cluster (Recommended for Development)

```bash
# Create KIND cluster and deploy everything
./k8s/scripts/deploy-kind.sh
```

This script will:
- Create a local KIND cluster
- Build the test Docker image
- Load the image into KIND
- Deploy all Kubernetes resources
- Run the integration tests

### Option 2: Deploy to Existing Cluster

```bash
# Build the Docker image
docker build -f k8s/Dockerfile.tests -t testcontainers-demo:latest .

# Deploy to your cluster
./k8s/scripts/deploy.sh
```

## 📁 Files Structure

```
k8s/
├── manifests/
│   ├── 01-namespace.yaml              # Creates testcontainers-demo namespace
│   ├── 02-rbac.yaml                   # ServiceAccount, Role, RoleBinding for Kubedock
│   ├── 03-configmap.yaml              # Environment variables and configuration
│   ├── 04-test-job.yaml               # Job using ConfigMap for source code (alternative)
│   └── 05-test-job-with-image.yaml    # Job using Docker image (recommended)
├── scripts/
│   ├── deploy.sh                      # Deploy to existing cluster
│   ├── deploy-kind.sh                 # Create KIND cluster and deploy
│   └── create-source-configmap.sh     # Alternative: create source code ConfigMap
├── Dockerfile.tests                   # Dockerfile for building test image
└── README.md                         # This file
```

## 🔧 Monitoring and Debugging

### View Job Status
```bash
kubectl get jobs -n testcontainers-demo -w
```

### Follow Logs (Both Containers)
```bash
kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -f --all-containers=true
```

### View Test Logs Only
```bash
kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c tests -f
```

### View Kubedock Logs
```bash
kubectl logs -n testcontainers-demo job/testcontainers-demo-job-image -c kubedock -f
```

### Check Pod Status
```bash
kubectl get pods -n testcontainers-demo
kubectl describe pod <pod-name> -n testcontainers-demo
```

### Debug Container Issues
```bash
# Get into the test container
kubectl exec -it <pod-name> -c tests -n testcontainers-demo -- /bin/bash

# Check Docker API from test container
kubectl exec -it <pod-name> -c tests -n testcontainers-demo -- wget -q -O - http://localhost:2375/version
```

## ⚙️ Configuration

### Testcontainers Configuration

The following environment variables are configured in the ConfigMap:

- `DOCKER_HOST=tcp://localhost:2375` - Points to Kubedock API
- `TESTCONTAINERS_RYUK_DISABLED=true` - Disables Ryuk (cleanup handled by Kubedock)
- `TESTCONTAINERS_CHECKS_DISABLE=true` - Disables Docker environment checks

### Kubedock Configuration

Key Kubedock settings:

- `--port-forward` - Enable port forwarding for container networking
- `--lock` - Enable namespace locking to prevent collisions
- `--prune-start` - Clean up any leftover resources on startup
- `--inspector` - Enable image inspection for better port detection
- `--request-cpu=100m,500m` - Set CPU requests and limits for test containers
- `--request-memory=128Mi,512Mi` - Set memory requests and limits

### Resource Requests/Limits

Test containers created by Kubedock will have:
- **CPU**: 100m request, 500m limit
- **Memory**: 128Mi request, 512Mi limit
- **Active Deadline**: 30 minutes (1800 seconds)

## 🔐 RBAC Permissions

The solution creates minimal RBAC permissions for Kubedock:

```yaml
# Core permissions
- pods: create, get, list, delete, watch
- pods/log: list, get  
- pods/exec: create
- services: create, get, list, delete
- configmaps: create, get, list, delete

# Optional: for namespace locking
- leases: create, get, update
```

## 🧪 How It Works

1. **Job Starts**: Kubernetes creates a pod with test and kubedock containers
2. **Kubedock Ready**: Kubedock sidecar starts and exposes Docker API on port 2375
3. **Tests Execute**: Test container runs `dotnet test` with Testcontainers
4. **Container Requests**: When tests need Redis, Testcontainers calls Docker API
5. **Pod Creation**: Kubedock receives the request and creates Redis pod in Kubernetes
6. **Networking**: Kubedock creates services for container communication
7. **Test Completion**: Tests run against real Redis pods, then cleanup happens automatically

## 🗂️ Alternative Deployments

### Using ConfigMap for Source Code

If you prefer not to build a Docker image:

```bash
./k8s/scripts/create-source-configmap.sh
kubectl apply -f k8s/manifests/04-test-job.yaml
```

### Custom Configuration

Edit `k8s/manifests/03-configmap.yaml` to customize:
- Resource requests/limits
- Container registry settings  
- Kubernetes labels/annotations
- Kubedock behavior

## 🧹 Cleanup

### Remove Everything
```bash
kubectl delete namespace testcontainers-demo
```

### Remove KIND Cluster
```bash
kind delete cluster --name testcontainers-demo
```

## 🎯 Benefits of This Approach

✅ **No Docker-in-Docker**: No privileged containers or docker socket mounting  
✅ **Resource Efficiency**: Test containers run as lightweight Kubernetes pods  
✅ **Scalability**: Can run multiple test suites in parallel  
✅ **Security**: No elevated privileges required  
✅ **Observability**: Full Kubernetes monitoring and logging  
✅ **CI/CD Ready**: Perfect for Tekton, GitHub Actions, GitLab CI, etc.  

## 🔗 References

- [Kubedock GitHub Repository](https://github.com/joyrex2001/kubedock)
- [Testcontainers Documentation](https://www.testcontainers.org/)
- [Kubernetes Jobs Documentation](https://kubernetes.io/docs/concepts/workloads/controllers/job/)

## 🐛 Troubleshooting

### Common Issues

**1. Image Pull Errors**
```bash
# For local clusters, ensure image is loaded
kind load docker-image testcontainers-demo:latest --name testcontainers-demo
```

**2. RBAC Permission Errors**
```bash
# Check ServiceAccount permissions
kubectl auth can-i create pods --as=system:serviceaccount:testcontainers-demo:kubedock-sa -n testcontainers-demo
```

**3. Kubedock Connection Issues**
```bash
# Check if kubedock is responding
kubectl exec -it <pod-name> -c tests -n testcontainers-demo -- wget -q --spider http://localhost:2375/version
```

**4. Test Container Resource Issues**
```bash
# Check pod resource usage
kubectl top pods -n testcontainers-demo
```

For more issues, check the [Kubedock documentation](https://github.com/joyrex2001/kubedock) and Kubernetes logs.
