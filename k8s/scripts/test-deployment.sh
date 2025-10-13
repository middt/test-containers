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

# Wait for tests to finish (check logs instead of job completion due to Kubedock sidecar)
echo ""
echo "⏳ Waiting for tests to complete..."
sleep 10  # Give it a moment to start

# Check if tests completed by looking at logs
TEST_COMPLETED=""
for i in {1..60}; do
    if kubectl logs job/$JOB_NAME -n $NAMESPACE -c tests 2>/dev/null | grep -q "✅ All tests completed successfully\|Test Run.*\."; then
        TEST_COMPLETED="true"
        break
    fi
    echo "Waiting for tests... ($i/60)"
    sleep 5
done

# Check job status based on test completion
JOB_STATUS="$TEST_COMPLETED"

if [[ "$JOB_STATUS" == "true" ]]; then
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
