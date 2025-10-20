import http from 'k6/http';
import { check, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const responseTime = new Trend('response_time', true);

// Smoke test - quick verification
export const options = {
  vus: 5, // 5 virtual users
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<300'], // Very strict for smoke test
    http_req_failed: ['rate<0.01'],   // Almost no errors allowed
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = 'smoke-test-client';

export default function () {
  group('Smoke Test - Basic Functionality', () => {
    // Test critical endpoints only
    const criticalEndpoints = [
      { path: '/health', name: 'Health Check', required: true },
      { path: '/ready', name: 'Readiness Check', required: true },
      { path: '/api/dashboard/status', name: 'Dashboard API', required: false },
    ];

    criticalEndpoints.forEach(endpoint => {
      let response = http.get(`${BASE_URL}${endpoint.path}`, {
        headers: { 
          'Client-Id': CLIENT_ID,
          'Accept': 'application/json'
        },
        timeout: '5s',
      });
      
      let checks = {
        [`${endpoint.name} - status OK`]: (r) => r.status === 200 || (!endpoint.required && r.status === 401),
        [`${endpoint.name} - fast response`]: (r) => r.timings.duration < 300,
        [`${endpoint.name} - no errors`]: (r) => r.status !== 0 && r.status < 500,
      };
      
      let success = check(response, checks);
      
      errorRate.add(!success);
      responseTime.add(response.timings.duration);
      
      if (endpoint.required && !success) {
        console.error(`❌ Critical endpoint ${endpoint.name} failed!`);
      }
    });
  });
}

export function handleSummary(data) {
  const allPassed = Object.values(data.thresholds).every(t => t.ok);
  
  return {
    'smoke-test-summary.json': JSON.stringify(data, null, 2),
    stdout: `
💨 Smoke Test Summary
====================
🎯 Purpose: Quick API validation
👥 Users: 5 concurrent
⏱️ Duration: 30 seconds
📊 Total Requests: ${data.metrics.http_reqs.values.count}
📈 Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
🚨 Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%

${allPassed ? '✅ SMOKE TEST PASSED - API is ready for further testing' : '❌ SMOKE TEST FAILED - Fix issues before load testing'}

Thresholds:
${Object.entries(data.thresholds).map(([key, value]) => 
  `${value.ok ? '✅' : '❌'} ${key}: ${value.ok ? 'PASSED' : 'FAILED'}`
).join('\n')}
    `,
  };
}