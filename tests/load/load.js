import http from 'k6/http';
import { check, sleep } from 'k6';

/**
 * Sustained load against authenticated read paths.
 *
 *   k6 run -e BASE_URL=https://localhost:7148 -e INSECURE=1 tests/load/load.js
 */
export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 25 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1500', 'p(99)<3000'],
  },
};

const baseUrl = (__ENV.BASE_URL || 'https://localhost:7148').replace(/\/$/, '');
const insecure = __ENV.INSECURE === '1';

export function setup() {
  const login = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({
      email: __ENV.EMAIL || 'admin@nexuscrm.local',
      password: __ENV.PASSWORD || 'ChangeMe!12345',
      tenantId: __ENV.TENANT_ID || '00000000-0000-0000-0000-000000000001',
    }),
    {
      headers: { 'Content-Type': 'application/json' },
      insecureSkipTLSVerify: insecure,
    },
  );

  if (login.status !== 200) {
    throw new Error(`Login failed: ${login.status} ${login.body}`);
  }

  return { token: login.json('accessToken') };
}

export default function (data) {
  const opts = {
    headers: { Authorization: `Bearer ${data.token}` },
    insecureSkipTLSVerify: insecure,
  };

  const paths = [
    '/api/v1/reports/summary',
    '/api/v1/reports/pipelines',
    '/api/v1/search?q=Acme',
    '/api/v1/leads/board',
    '/api/v1/deals/board',
    '/api/v1/customers?page=1&pageSize=20',
  ];

  const path = paths[Math.floor(Math.random() * paths.length)];
  const res = http.get(`${baseUrl}${path}`, opts);
  check(res, { 'ok': (r) => r.status === 200 });
  sleep(0.2);
}
