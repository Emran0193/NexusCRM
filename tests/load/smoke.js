import http from 'k6/http';
import { check, sleep } from 'k6';

/**
 * Smoke: health + login + authenticated search/reports.
 *
 *   k6 run -e BASE_URL=https://localhost:7148 -e INSECURE=1 tests/load/smoke.js
 */
export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
  },
};

const baseUrl = (__ENV.BASE_URL || 'https://localhost:7148').replace(/\/$/, '');
const insecure = __ENV.INSECURE === '1';

function opts(extra = {}) {
  return {
    insecureSkipTLSVerify: insecure,
    ...extra,
  };
}

export default function () {
  const live = http.get(`${baseUrl}/health/live`, opts());
  check(live, { 'health live 200': (r) => r.status === 200 });

  const login = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({
      email: __ENV.EMAIL || 'admin@nexuscrm.local',
      password: __ENV.PASSWORD || 'ChangeMe!12345',
      tenantId: __ENV.TENANT_ID || '00000000-0000-0000-0000-000000000001',
    }),
    opts({ headers: { 'Content-Type': 'application/json' } }),
  );

  check(login, { 'login 200': (r) => r.status === 200 });
  const token = login.json('accessToken');

  const auth = opts({
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  check(http.get(`${baseUrl}/api/v1/search?q=Acme`, auth), { 'search 200': (r) => r.status === 200 });
  check(http.get(`${baseUrl}/api/v1/reports/summary`, auth), { 'reports 200': (r) => r.status === 200 });
  check(http.get(`${baseUrl}/api/v1/leads/board`, auth), { 'leads board 200': (r) => r.status === 200 });

  sleep(0.5);
}
