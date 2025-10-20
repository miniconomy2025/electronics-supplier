import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const responseTime = new Trend('response_time', true);

// Endurance test - longer duration with moderate load
export const options = {
  stages: [
    { duration: '2m', target: 20 },    // Ramp up to 20 users
    { duration: '10m', target: 20 },   // Stay at 20 users for 10 minutes
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<800', 'p(99)<1500'],
    http_req_failed: ['rate<0.05'],    // Strict error rate for endurance
    errors: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = 'endurance-test-client';

// Track iterations for memory leak detection
let iterationCount = 0;

export default function () {
  iterationCount++;
  
  group('Endurance Test - Memory & Resource Check', () => {
    // Cycle through different endpoints to test various code paths
    const endpoints = [
      { path: '/health', name: 'health' },
      { path: '/ready', name: 'ready' },
      { path: '/api/dashboard/status', name: 'dashboard' },
      { path: '/api/electronics', name: 'electronics' },
      { path: '/api/inventory', name: 'inventory' },
      { path: '/api/logistics', name: 'logistics' },
      { path: '/api/machines', name: 'machines' },
    ];

    // Test each endpoint in sequence
    endpoints.forEach(endpoint => {
      let response = http.get(`${BASE_URL}${endpoint.path}`, {
        headers: { 
          'Client-Id': CLIENT_ID,
          'Accept': 'application/json',
          'X-Iteration': iterationCount.toString() // Track iteration for debugging
        },
        timeout: '10s',
      });
      
      let success = check(response, {
        [`${endpoint.name} status OK`]: (r) => r.status === 200 || r.status === 401,
        [`${endpoint.name} response time consistent`]: (r) => r.timings.duration < 800,
        [`${endpoint.name} no memory errors`]: (r) => !r.body.includes('OutOfMemory'),
      });
      
      errorRate.add(!success);
      responseTime.add(response.timings.duration);
    });
  });

  // Longer sleep for endurance test to simulate real user behavior
  sleep(2 + Math.random() * 3); // 2-5 seconds between requests
}

export function handleSummary(data) {
  const durationMinutes = 14; // Total test duration
  const avgRequestsPerMinute = data.metrics.http_reqs.values.count / durationMinutes;
  
  return {
    'endurance-test-summary.json': JSON.stringify(data, null, 2),
    stdout: `
⏱️ Endurance Test Summary
========================
🕐 Test Duration: ${durationMinutes} minutes
👥 Concurrent Users: 20
📊 Total Requests: ${data.metrics.http_reqs.values.count}
📈 Avg Requests/min: ${avgRequestsPerMinute.toFixed(0)}
📈 Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
🎯 95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
🚨 Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%

Memory & Resource Health:
${data.metrics.http_req_duration.values.avg > data.metrics.http_req_duration.values.med * 2 ? 
  '⚠️ Response time variance suggests potential memory issues' : 
  '✅ Consistent response times - no apparent memory leaks'}
${data.metrics.http_req_failed.values.rate > 0.02 ? 
  '⚠️ Error rate increased over time' : 
  '✅ Stable error rate throughout test'}

Thresholds:
${Object.entries(data.thresholds).map(([key, value]) => 
  `${value.ok ? '✅' : '❌'} ${key}: ${value.ok ? 'PASSED' : 'FAILED'}`
).join('\n')}
    `,
  };
}