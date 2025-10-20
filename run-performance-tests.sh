#!/bin/bash

# Performance testing script for Electronics Supplier API
# Usage: ./run-performance-tests.sh [test-type] [base-url]

set -e

# Configuration
TEST_TYPE=${1:-"smoke"}
BASE_URL=${2:-"http://localhost:5000"}
RESULTS_DIR="./performance-results"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if k6 is installed
check_k6() {
    if ! command -v k6 &> /dev/null; then
        print_error "k6 is not installed. Please install k6 first:"
        echo ""
        echo "Windows (Chocolatey): choco install k6"
        echo "macOS (Homebrew): brew install k6"
        echo "Linux: See https://k6.io/docs/getting-started/installation/"
        exit 1
    fi
    print_success "k6 is installed"
}

# Check if API is running and AWS safety
check_api() {
    print_status "Checking if API is running at $BASE_URL..."
    
    # Check if URL suggests AWS hosting
    if echo "$BASE_URL" | grep -qE "(\.amazonaws\.com|\.aws\.|ec2-|elasticbeanstalk|\.compute\.amazonaws\.com)"; then
        if [ "$TEST_TYPE" != "smoke" ] && [ "$TEST_TYPE" != "help" ]; then
            print_error "🚨 AWS-HOSTED ENDPOINT DETECTED!"
            print_warning "Target: $BASE_URL"
            echo ""
            print_warning "CAUTION: Load testing AWS-hosted applications can:"
            print_warning "- Trigger DDoS protection mechanisms"
            print_warning "- Result in IP blacklisting"
            print_warning "- Generate unexpected AWS charges"
            print_warning "- Violate AWS Acceptable Use Policy"
            echo ""
            print_warning "RECOMMENDED:"
            print_warning "- Use 'smoke' test only for AWS environments"
            print_warning "- Get AWS Support approval for load testing"
            print_warning "- Use dedicated staging environment"
            print_warning "- Consider AWS Load Testing solution instead"
            echo ""
            print_warning "See performance-tests/AWS-SAFETY-GUIDE.md for details"
            echo ""
            
            read -p "Continue anyway? (yes/no): " response
            if [ "$response" != "yes" ]; then
                print_error "Aborting for safety. Use 'smoke' test or local environment."
                exit 1
            fi
        fi
    fi
    
    for i in {1..10}; do
        if curl -s "$BASE_URL/health" >/dev/null 2>&1; then
            print_success "API is running and responding"
            
            # Additional warning for AWS
            if echo "$BASE_URL" | grep -qE "(\.amazonaws\.com|\.aws\.|ec2-|elasticbeanstalk)"; then
                print_warning "⚠️  Remember: Monitor CloudWatch for any throttling during test"
            fi
            return 0
        fi
        
        if [ $i -eq 10 ]; then
            print_error "API is not responding at $BASE_URL"
            print_warning "Please start your API first:"
            print_warning "  cd esAPI && dotnet run"
            exit 1
        fi
        
        print_status "Waiting for API... (attempt $i/10)"
        sleep 2
    done
}

# Create results directory
setup_results_dir() {
    mkdir -p "$RESULTS_DIR"
    print_status "Results will be saved to: $RESULTS_DIR"
}

# Run specific test
run_test() {
    local test_name=$1
    local test_file="performance-tests/${test_name}.js"
    
    if [ ! -f "$test_file" ]; then
        print_error "Test file not found: $test_file"
        exit 1
    fi
    
    print_status "Running $test_name..."
    print_status "Target: $BASE_URL"
    
    # Run the test with results output
    cd performance-tests
    if k6 run \
        --out json="../${RESULTS_DIR}/${test_name}-results.json" \
        -e BASE_URL="$BASE_URL" \
        "$test_name.js"; then
        
        print_success "$test_name completed successfully"
        
        # Move summary file if it exists
        if [ -f "${test_name}-summary.json" ]; then
            mv "${test_name}-summary.json" "../${RESULTS_DIR}/"
        fi
        
        cd ..
        return 0
    else
        print_error "$test_name failed"
        cd ..
        return 1
    fi
}

# Display results summary
show_summary() {
    local test_name=$1
    local results_file="${RESULTS_DIR}/${test_name}-results.json"
    
    if [ -f "$results_file" ] && command -v jq &> /dev/null; then
        print_status "📊 $test_name Summary:"
        
        # Extract key metrics
        local avg_response=$(jq -r '.metrics.http_req_duration.values.avg' "$results_file" 2>/dev/null || echo "N/A")
        local p95_response=$(jq -r '.metrics.http_req_duration.values["p(95)"]' "$results_file" 2>/dev/null || echo "N/A")
        local error_rate=$(jq -r '.metrics.http_req_failed.values.rate' "$results_file" 2>/dev/null || echo "N/A")
        local total_requests=$(jq -r '.metrics.http_reqs.values.count' "$results_file" 2>/dev/null || echo "N/A")
        
        echo "  Average Response Time: ${avg_response}ms"
        echo "  95th Percentile: ${p95_response}ms"
        echo "  Error Rate: $(echo "$error_rate * 100" | bc -l 2>/dev/null || echo "$error_rate")%"
        echo "  Total Requests: $total_requests"
        echo ""
    fi
}

# Print usage
print_usage() {
    echo "Usage: $0 [test-type] [base-url]"
    echo ""
    echo "Test Types:"
    echo "  smoke      - Quick validation (5 users, 30s)"
    echo "  load       - Normal load testing (0→10→0 users)"
    echo "  stress     - Stress testing (0→150 users)"
    echo "  spike      - Spike testing (sudden load spikes)"
    echo "  endurance  - Long-running test (20 users, 14min)"
    echo "  all        - Run all tests sequentially"
    echo ""
    echo "Examples:"
    echo "  $0 smoke"
    echo "  $0 load http://localhost:5000"
    echo "  $0 all https://your-api.com"
}

# Main execution
main() {
    echo "🚀 Electronics Supplier Performance Testing"
    echo "==========================================="
    
    # Validate arguments
    case "$TEST_TYPE" in
        smoke|load|stress|spike|endurance|all)
            ;;
        help|--help|-h)
            print_usage
            exit 0
            ;;
        *)
            print_error "Invalid test type: $TEST_TYPE"
            print_usage
            exit 1
            ;;
    esac
    
    # Setup
    check_k6
    setup_results_dir
    check_api
    
    print_status "Starting performance tests..."
    print_status "Test type: $TEST_TYPE"
    print_status "Base URL: $BASE_URL"
    echo ""
    
    # Run tests
    if [ "$TEST_TYPE" = "all" ]; then
        # Run all tests
        tests=("smoke-test" "load-test" "stress-test" "spike-test" "endurance-test")
        failed_tests=()
        
        for test in "${tests[@]}"; do
            print_status "🔄 Running $test..."
            if run_test "$test"; then
                show_summary "$test"
            else
                failed_tests+=("$test")
            fi
            
            # Add delay between tests
            if [ "$test" != "endurance-test" ]; then
                print_status "Waiting 30 seconds before next test..."
                sleep 30
            fi
        done
        
        # Summary
        echo "🏁 All tests completed!"
        if [ ${#failed_tests[@]} -eq 0 ]; then
            print_success "All tests passed!"
        else
            print_warning "Some tests failed: ${failed_tests[*]}"
        fi
        
    else
        # Run single test
        test_file="${TEST_TYPE}-test"
        if run_test "$test_file"; then
            show_summary "$test_file"
            print_success "Test completed successfully!"
        else
            print_error "Test failed!"
            exit 1
        fi
    fi
    
    # Final information
    echo ""
    print_status "📁 Results saved to: $RESULTS_DIR"
    print_status "📊 View detailed results:"
    echo "  - JSON results: $RESULTS_DIR/${TEST_TYPE}-test-results.json"
    echo "  - Summary: $RESULTS_DIR/${TEST_TYPE}-test-summary.json"
    echo ""
    print_success "Performance testing completed! 🎉"
}

# Run main function
main "$@"