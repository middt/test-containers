#!/bin/bash

# Deploy to local KIND cluster for development/testing
set -e

CLUSTER_NAME="testcontainers-demo"
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

echo "🚀 Setting up local KIND cluster for Testcontainers Demo"
echo "====================================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "🔍 Checking prerequisites..."
if ! command_exists kind; then
    echo "❌ kind is required but not installed"
    echo "Install from: https://kind.sigs.k8s.io/docs/user/quick-start/"
    exit 1
fi

if ! command_exists docker; then
    echo "❌ docker is required but not installed"
    exit 1
fi

if ! command_exists kubectl; then
    echo "❌ kubectl is required but not installed"
    exit 1
fi

echo "✅ Prerequisites checked"

# Create KIND cluster if it doesn't exist
if ! kind get clusters | grep -q "^${CLUSTER_NAME}$"; then
    echo ""
    echo "🔧 Creating KIND cluster: $CLUSTER_NAME"
    
    # Create cluster config
    cat <<EOF > /tmp/kind-config.yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  image: kindest/node:v1.29.0
  extraPortMappings:
  - containerPort: 30080
    hostPort: 8080
    protocol: TCP
  kubeadmConfigPatches:
  - |
    kind: InitConfiguration
    nodeRegistration:
      kubeletExtraArgs:
        node-labels: "ingress-ready=true"
EOF

    kind create cluster --name "$CLUSTER_NAME" --config /tmp/kind-config.yaml
    rm /tmp/kind-config.yaml
    echo "✅ KIND cluster created"
else
    echo "✅ KIND cluster '$CLUSTER_NAME' already exists"
fi

# Switch kubectl context
kubectl cluster-info --context kind-$CLUSTER_NAME
kubectl config use-context kind-$CLUSTER_NAME

# Build and load the test image into KIND
echo ""
echo "🏗️  Building and loading test image into KIND cluster..."
cd "$PROJECT_ROOT"
docker build -f k8s/Dockerfile.tests -t testcontainers-demo:latest .
kind load docker-image testcontainers-demo:latest --name "$CLUSTER_NAME"
echo "✅ Test image loaded into KIND cluster"

# Deploy using the main deploy script
echo ""
echo "📦 Deploying to KIND cluster..."
bash "$PROJECT_ROOT/k8s/scripts/deploy.sh"

echo ""
echo "🎉 Deployment to KIND cluster completed!"
echo ""
echo "🔧 KIND-specific commands:"
echo "  # Switch to cluster context"
echo "  kubectl config use-context kind-$CLUSTER_NAME"
echo ""
echo "  # Delete the cluster when done"
echo "  kind delete cluster --name $CLUSTER_NAME"
echo ""
echo "  # Load updated image after changes"
echo "  docker build -f k8s/Dockerfile.tests -t testcontainers-demo:latest . && kind load docker-image testcontainers-demo:latest --name $CLUSTER_NAME"
