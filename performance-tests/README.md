# Performance Testing Configuration

This directory contains k6 performance tests for the Electronics Supplier API.

## ⚠️ AWS Safety Notice

**IMPORTANT**: When testing against AWS-hosted applications, be aware of:

- Rate limiting and throttling policies
- AWS Shield DDoS protection triggers  
- Potential IP blacklisting from aggressive testing
- AWS WAF rules that may block repeated requests

**Recommendations for AWS environments:**

- Get approval from AWS Support for load testing
- Use dedicated staging environments for performance testing
- Consider AWS Load Testing solution for approved testing
- Implement synthetic monitoring instead of aggressive load testing
- Monitor CloudWatch for throttling and rate limiting

## Test Types

### 1. Smoke Test (`smoke-test.js`)

**Purpose**: Quick validation that the API is functional

- **Duration**: 30 seconds
- **Users**: 5 concurrent
- **Thresholds**: Very strict (95% < 300ms, <1% errors)
- **Use Case**: Pre-deployment validation, CI/CD gates

### 2. Load Test (`load-test.js`)

**Purpose**: Test normal expected load

- **Duration**: ~2 minutes
- **Users**: Ramp 0→10→10→0
- **Thresholds**: 95% < 500ms, 99% < 1s, <10% errors
- **Use Case**: Baseline performance measurement

### 3. Stress Test (`stress-test.js`)

**Purpose**: Find the breaking point

- **Duration**: ~12 minutes  
- **Users**: Ramp 0→50→100→150→0
- **Thresholds**: Relaxed for high load
- **Use Case**: Capacity planning, identify limits

### 4. Spike Test (`spike-test.js`)

**Purpose**: Test resilience to sudden traffic spikes

- **Duration**: ~3 minutes
- **Users**: Sudden spikes to 200, then 300 users
- **Thresholds**: Very relaxed during spikes
- **Use Case**: Test auto-scaling, circuit breakers

### 5. Endurance Test (`endurance-test.js`)

**Purpose**: Test for memory leaks and resource exhaustion

- **Duration**: 14 minutes
- **Users**: 20 concurrent (steady state)
- **Thresholds**: Strict error rates over time
- **Use Case**: Production readiness, memory leak detection

## Running Tests Locally

### Prerequisites

```bash
# Install k6
# Windows (Chocolatey)
choco install k6

# macOS (Homebrew)
brew install k6

# Linux
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Running Individual Tests

```bash
# Start your API first
cd esAPI
dotnet run

# In another terminal, run performance tests
cd performance-tests

# Smoke test (quick validation)
k6 run smoke-test.js

# Load test (normal load)
k6 run load-test.js

# Stress test (find limits)
k6 run stress-test.js

# Spike test (sudden load)
k6 run spike-test.js

# Endurance test (memory leaks)
k6 run endurance-test.js
```

### Custom Configuration

```bash
# Test against different environment
k6 run -e BASE_URL=https://your-api.com load-test.js

# Override user count
k6 run --vus 20 --duration 60s smoke-test.js

# Save detailed results
k6 run --out json=results.json load-test.js
```

## Metrics Explained

### Response Time Metrics

- **Average**: Mean response time across all requests
- **p(95)**: 95% of requests complete within this time
- **p(99)**: 99% of requests complete within this time
- **Max**: Slowest request in the test

### Throughput Metrics

- **Requests/sec**: Average requests handled per second
- **Total Requests**: Total number of requests made during test

### Error Metrics

- **Error Rate**: Percentage of failed requests
- **Status Codes**: Distribution of HTTP response codes

## Performance Targets

### Recommended Thresholds

| Test Type | 95th Percentile | 99th Percentile | Error Rate | Notes |
|-----------|----------------|-----------------|------------|--------|
| Smoke     | < 300ms        | < 500ms         | < 1%       | Very strict |
| Load      | < 500ms        | < 1000ms        | < 10%      | Normal operation |
| Stress    | < 1000ms       | < 2000ms        | < 20%      | Under stress |
| Spike     | < 2000ms       | < 5000ms        | < 30%      | During spikes |
| Endurance | < 800ms        | < 1500ms        | < 5%       | Sustained load |

### Key Performance Indicators (KPIs)

1. **Availability**: API should respond (not return 0 status)
2. **Response Time**: Meet SLA requirements
3. **Throughput**: Handle expected load
4. **Error Rate**: Stay within acceptable limits
5. **Resource Efficiency**: No memory leaks over time

## Interpreting Results

### ✅ Good Performance

- All thresholds pass
- Consistent response times
- Low error rates
- Linear scaling with load

### ⚠️ Performance Issues

- Thresholds failing
- Increasing response times under load
- High error rates
- Memory usage growing over time

### ❌ Critical Issues

- API not responding (status 0)
- 5xx server errors
- Response times > 5 seconds
- Error rates > 50%

## Integration with CI/CD

These tests are integrated into the GitHub Actions pipeline:

- **Smoke tests**: Run on every PR (gate for deployment)
- **Load tests**: Run on main branch pushes
- **Full suite**: Run nightly for comprehensive monitoring

See `.github/workflows/performance.yml` for pipeline configuration.
