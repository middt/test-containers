#!/bin/bash

# Test script to verify the Kubernetes deployment
set -e

NAMESPACE="testcontainers-demo"
JOB_NAME="testcontainers-demo-job-image"

echo "🧪 Testing Kubernetes Deployment"
echo "================================"

# Check if namespace exists
if ! kubectl get namespace $NAMESPACE >/dev/null 2>&1; then
    echo "❌ Namespace '$NAMESPACE' does not exist. Please run deploy.sh first."
    exit 1
fi

# Check if job exists
if ! kubectl get job $JOB_NAME -n $NAMESPACE >/dev/null 2>&1; then
    echo "❌ Job '$JOB_NAME' does not exist. Please run deploy.sh first."
    exit 1
fi

echo "✅ Namespace and job found"

# Wait for job to complete
echo ""
echo "⏳ Waiting for job to complete..."
kubectl wait --for=condition=complete job/$JOB_NAME -n $NAMESPACE --timeout=600s

# Check job status
JOB_STATUS=$(kubectl get job $JOB_NAME -n $NAMESPACE -o jsonpath='{.status.conditions[?(@.type=="Complete")].status}')

if [[ "$JOB_STATUS" == "True" ]]; then
    echo "✅ Job completed successfully!"
    
    echo ""
    echo "📊 Job Summary:"
    kubectl get job $JOB_NAME -n $NAMESPACE
    
    echo ""
    echo "📋 Pod Information:"
    kubectl get pods -n $NAMESPACE -l job-name=$JOB_NAME
    
    echo ""
    echo "📝 Test Results (Last 50 lines):"
    echo "================================"
    kubectl logs -n $NAMESPACE job/$JOB_NAME -c tests --tail=50
    
    echo ""
    echo "🎉 All tests passed! The Kubernetes deployment is working correctly."
    
else
    echo "❌ Job failed or did not complete"
    
    echo ""
    echo "📋 Job Status:"
    kubectl get job $JOB_NAME -n $NAMESPACE
    
    echo ""
    echo "📋 Pod Status:"
    kubectl get pods -n $NAMESPACE -l job-name=$JOB_NAME
    
    echo ""
    echo "📝 Test Logs:"
    echo "============="
    kubectl logs -n $NAMESPACE job/$JOB_NAME -c tests || echo "No test logs available"
    
    echo ""
    echo "📝 Kubedock Logs:"
    echo "================="
    kubectl logs -n $NAMESPACE job/$JOB_NAME -c kubedock || echo "No kubedock logs available"
    
    exit 1
fi
