import http from 'k6/http';
import { check, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const responseTime = new Trend('response_time', true);

// Stress test configuration
export const options = {
  stages: [
    { duration: '1m', target: 50 },    // Ramp up to 50 users
    { duration: '3m', target: 50 },    // Stay at 50 users  
    { duration: '1m', target: 100 },   // Ramp up to 100 users
    { duration: '3m', target: 100 },   // Stay at 100 users
    { duration: '1m', target: 150 },   // Ramp up to 150 users
    { duration: '2m', target: 150 },   // Stay at 150 users
    { duration: '1m', target: 0 },     // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000', 'p(99)<2000'], // More relaxed for stress test
    http_req_failed: ['rate<0.2'],                    // Allow higher error rate
    errors: ['rate<0.2'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = 'stress-test-client';

export default function () {
  group('Stress Test - Core Endpoints', () => {
    // Focus on the most critical endpoints
    const endpoints = [
      '/health',
      '/ready',
      '/api/dashboard/status',
      '/api/electronics',
      '/api/inventory'
    ];

    // Test random endpoint
    const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
    
    let response = http.get(`${BASE_URL}${endpoint}`, {
      headers: { 
        'Client-Id': CLIENT_ID,
        'Accept': 'application/json'
      },
      timeout: '10s', // Longer timeout for stress test
    });
    
    let success = check(response, {
      'status is not 5xx': (r) => r.status < 500,
      'response time under 2s': (r) => r.timings.duration < 2000,
      'no connection errors': (r) => r.status !== 0,
    });
    
    errorRate.add(!success);
    responseTime.add(response.timings.duration);
  });
}

export function handleSummary(data) {
  return {
    'stress-test-summary.json': JSON.stringify(data, null, 2),
    stdout: `
🔥 Stress Test Summary
=====================
🎯 Peak Users: 150 concurrent
📊 Total Requests: ${data.metrics.http_reqs.values.count}
📈 Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
🎯 95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
🚨 Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%
🔥 Peak Requests/sec: ${data.metrics.http_reqs.values.rate.toFixed(2)}

System Stress Indicators:
${data.metrics.http_req_duration.values['p(99)'] > 2000 ? '⚠️ High 99th percentile latency detected' : '✅ Response times within acceptable range'}
${data.metrics.http_req_failed.values.rate > 0.1 ? '⚠️ Elevated error rate under stress' : '✅ Error rate acceptable under stress'}

Thresholds:
${Object.entries(data.thresholds).map(([key, value]) => 
  `${value.ok ? '✅' : '❌'} ${key}: ${value.ok ? 'PASSED' : 'FAILED'}`
).join('\n')}
    `,
  };
}