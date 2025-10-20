import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const responseTime = new Trend('response_time', true);
export const throughput = new Counter('requests_total');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up to 10 users
    { duration: '1m', target: 10 },    // Stay at 10 users
    { duration: '20s', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'], // 95% of requests under 500ms, 99% under 1s
    http_req_failed: ['rate<0.1'],                   // Error rate under 10%
    errors: ['rate<0.1'],                           // Custom error rate under 10%
  },
};

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = 'load-test-client';

export default function () {
  group('Health Checks', () => {
    // Health endpoint
    let healthRes = http.get(`${BASE_URL}/health`, {
      headers: { 'Client-Id': CLIENT_ID },
    });
    
    let healthCheck = check(healthRes, {
      'health status is 200': (r) => r.status === 200,
      'health response time < 100ms': (r) => r.timings.duration < 100,
      'health contains healthy status': (r) => r.body.includes('Healthy') || r.body.includes('healthy'),
    });
    
    errorRate.add(!healthCheck);
    responseTime.add(healthRes.timings.duration);
    throughput.add(1);

    // Ready endpoint
    let readyRes = http.get(`${BASE_URL}/ready`, {
      headers: { 'Client-Id': CLIENT_ID },
    });
    
    let readyCheck = check(readyRes, {
      'ready status is 200': (r) => r.status === 200,
      'ready response time < 100ms': (r) => r.timings.duration < 100,
    });
    
    errorRate.add(!readyCheck);
    responseTime.add(readyRes.timings.duration);
    throughput.add(1);
  });

  group('API Endpoints', () => {
    // Dashboard status
    let dashboardRes = http.get(`${BASE_URL}/api/dashboard/status`, {
      headers: { 
        'Client-Id': CLIENT_ID,
        'Accept': 'application/json'
      },
    });
    
    let dashboardCheck = check(dashboardRes, {
      'dashboard status is 200 or 401': (r) => r.status === 200 || r.status === 401,
      'dashboard response time < 500ms': (r) => r.timings.duration < 500,
      'dashboard returns JSON': (r) => r.headers['Content-Type'] && r.headers['Content-Type'].includes('json'),
    });
    
    errorRate.add(!dashboardCheck);
    responseTime.add(dashboardRes.timings.duration);
    throughput.add(1);

    // Electronics inventory
    let electronicsRes = http.get(`${BASE_URL}/api/electronics`, {
      headers: { 
        'Client-Id': CLIENT_ID,
        'Accept': 'application/json'
      },
    });
    
    let electronicsCheck = check(electronicsRes, {
      'electronics status is 200 or 401': (r) => r.status === 200 || r.status === 401,
      'electronics response time < 500ms': (r) => r.timings.duration < 500,
    });
    
    errorRate.add(!electronicsCheck);
    responseTime.add(electronicsRes.timings.duration);
    throughput.add(1);

    // Inventory endpoint
    let inventoryRes = http.get(`${BASE_URL}/api/inventory`, {
      headers: { 
        'Client-Id': CLIENT_ID,
        'Accept': 'application/json'
      },
    });
    
    let inventoryCheck = check(inventoryRes, {
      'inventory status is 200 or 401': (r) => r.status === 200 || r.status === 401,
      'inventory response time < 500ms': (r) => r.timings.duration < 500,
    });
    
    errorRate.add(!inventoryCheck);
    responseTime.add(inventoryRes.timings.duration);
    throughput.add(1);
  });

  // Wait between iterations
  sleep(1);
}

export function handleSummary(data) {
  return {
    'performance-summary.json': JSON.stringify(data, null, 2),
    stdout: `
📊 Performance Test Summary
==========================
✅ Total Requests: ${data.metrics.http_reqs.values.count}
📈 Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
🎯 95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
🚨 Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%
🔥 Requests/sec: ${data.metrics.http_reqs.values.rate.toFixed(2)}

Thresholds:
${Object.entries(data.thresholds).map(([key, value]) => 
  `${value.ok ? '✅' : '❌'} ${key}: ${value.ok ? 'PASSED' : 'FAILED'}`
).join('\n')}
    `,
  };
}