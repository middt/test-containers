#!/bin/bash

# Script to create ConfigMap with source code
set -e

NAMESPACE="testcontainers-demo"
CONFIGMAP_NAME="source-code"
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

echo "Creating source code ConfigMap..."
echo "Project root: $PROJECT_ROOT"

# Create the namespace if it doesn't exist
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Create ConfigMap with the source code
kubectl create configmap $CONFIGMAP_NAME \
  --namespace=$NAMESPACE \
  --from-file="$PROJECT_ROOT" \
  --recursive \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Source code ConfigMap '$CONFIGMAP_NAME' created in namespace '$NAMESPACE'"
echo ""
echo "Files included in ConfigMap:"
kubectl get configmap $CONFIGMAP_NAME -n $NAMESPACE -o jsonpath='{.data}' | jq -r 'keys[]' 2>/dev/null || echo "ConfigMap created successfully (jq not available for detailed listing)"
