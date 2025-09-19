#!/bin/bash

# Deployment script for Testcontainers Demo with Kubedock
set -e

NAMESPACE="testcontainers-demo"
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
K8S_DIR="$PROJECT_ROOT/k8s"

echo "🚀 Deploying Testcontainers Demo with Kubedock to Kubernetes"
echo "=================================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Verify prerequisites
echo "🔍 Checking prerequisites..."
if ! command_exists kubectl; then
    echo "❌ kubectl is required but not installed"
    exit 1
fi

if ! command_exists docker; then
    echo "❌ docker is required but not installed" 
    exit 1
fi

echo "✅ Prerequisites checked"

# Check if we're connected to a cluster
if ! kubectl cluster-info >/dev/null 2>&1; then
    echo "❌ Not connected to a Kubernetes cluster"
    echo "Please configure kubectl to connect to your cluster"
    exit 1
fi

echo "✅ Connected to Kubernetes cluster: $(kubectl config current-context)"

# Build the test image
echo ""
echo "🏗️  Building test Docker image..."
cd "$PROJECT_ROOT"
docker build -f k8s/Dockerfile.tests -t testcontainers-demo:latest .
echo "✅ Docker image built successfully"

# Apply Kubernetes manifests
echo ""
echo "📦 Applying Kubernetes manifests..."

# Apply in order
kubectl apply -f "$K8S_DIR/manifests/01-namespace.yaml"
echo "✅ Namespace created"

kubectl apply -f "$K8S_DIR/manifests/02-rbac.yaml"
echo "✅ RBAC resources created"

kubectl apply -f "$K8S_DIR/manifests/03-configmap.yaml"
echo "✅ ConfigMap created"

# Clean up any existing jobs
echo ""
echo "🧹 Cleaning up any existing test jobs..."
kubectl delete job testcontainers-demo-job-image -n $NAMESPACE --ignore-not-found=true
kubectl wait --for=delete job/testcontainers-demo-job-image -n $NAMESPACE --timeout=60s 2>/dev/null || true

# Apply the test job
kubectl apply -f "$K8S_DIR/manifests/05-test-job-with-image.yaml"
echo "✅ Test job created"

echo ""
echo "🎯 Deployment completed!"
echo ""
echo "📋 Next steps:"
echo "1. Monitor the job: kubectl get jobs -n $NAMESPACE -w"
echo "2. View logs: kubectl logs -n $NAMESPACE job/testcontainers-demo-job-image -f"
echo "3. Check pod status: kubectl get pods -n $NAMESPACE"
echo ""
echo "🔧 Useful commands:"
echo "  # Follow logs from both containers"
echo "  kubectl logs -n $NAMESPACE job/testcontainers-demo-job-image -f --all-containers=true"
echo ""
echo "  # Get just test logs"
echo "  kubectl logs -n $NAMESPACE job/testcontainers-demo-job-image -c tests -f"
echo ""  
echo "  # Get kubedock logs"
echo "  kubectl logs -n $NAMESPACE job/testcontainers-demo-job-image -c kubedock -f"
echo ""
echo "  # Clean up"
echo "  kubectl delete namespace $NAMESPACE"
