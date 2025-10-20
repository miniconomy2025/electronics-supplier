import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const responseTime = new Trend('response_time', true);

// Spike test configuration - sudden traffic spikes
export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Normal load
    { duration: '10s', target: 200 },  // Sudden spike!
    { duration: '30s', target: 200 },  // Stay at spike
    { duration: '10s', target: 10 },   // Back to normal
    { duration: '30s', target: 10 },   // Normal operations
    { duration: '10s', target: 300 },  // Even bigger spike!
    { duration: '20s', target: 300 },  // Stay at big spike
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // Very relaxed for spike test
    http_req_failed: ['rate<0.3'],     // Allow higher error rate during spikes
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = 'spike-test-client';

export default function () {
  group('Spike Test - Resilience Check', () => {
    // Test health endpoint (should always work)
    let healthRes = http.get(`${BASE_URL}/health`, {
      headers: { 'Client-Id': CLIENT_ID },
      timeout: '15s',
    });
    
    let healthCheck = check(healthRes, {
      'health responds': (r) => r.status !== 0,
      'health not server error': (r) => r.status < 500,
    });
    
    errorRate.add(!healthCheck);
    responseTime.add(healthRes.timings.duration);

    // Test API endpoint
    let apiRes = http.get(`${BASE_URL}/api/dashboard/status`, {
      headers: { 
        'Client-Id': CLIENT_ID,
        'Accept': 'application/json'
      },
      timeout: '15s',
    });
    
    let apiCheck = check(apiRes, {
      'api responds': (r) => r.status !== 0,
      'api not server error': (r) => r.status < 500,
      'response within 5s': (r) => r.timings.duration < 5000,
    });
    
    errorRate.add(!apiCheck);
    responseTime.add(apiRes.timings.duration);
  });

  sleep(0.1); // Short sleep during spike test
}

export function handleSummary(data) {
  return {
    'spike-test-summary.json': JSON.stringify(data, null, 2),
    stdout: `
⚡ Spike Test Summary
===================
🎯 Peak Spike: 300 concurrent users
📊 Total Requests: ${data.metrics.http_reqs.values.count}
📈 Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
🎯 95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
🚨 Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%

Spike Resilience:
${data.metrics.http_req_duration.values['p(95)'] > 5000 ? '⚠️ System shows stress under spike load' : '✅ System handles spikes well'}
${data.metrics.http_req_failed.values.rate > 0.2 ? '⚠️ High error rate during spikes' : '✅ Low error rate during spikes'}

Thresholds:
${Object.entries(data.thresholds).map(([key, value]) => 
  `${value.ok ? '✅' : '❌'} ${key}: ${value.ok ? 'PASSED' : 'FAILED'}`
).join('\n')}
    `,
  };
}