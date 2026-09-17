# NexusCRM load tests (k6)

Requires [k6](https://k6.io/docs/get-started/installation/) and a running API (with demo seed).

```bash
# Terminal A
docker compose -f deploy/docker-compose.yml up -d postgres
dotnet run --project src/NexusCRM.Api

# Terminal B — smoke (1 iteration)
k6 run -e BASE_URL=https://localhost:7148 -e INSECURE=1 tests/load/smoke.js

# Sustained load (~2 minutes)
k6 run -e BASE_URL=https://localhost:7148 -e INSECURE=1 tests/load/load.js
```

| Script | Purpose |
|--------|---------|
| `smoke.js` | Health, login, search, reports, lead board |
| `load.js` | Ramps to 25 VUs on authenticated read endpoints |

Optional env: `EMAIL`, `PASSWORD`, `TENANT_ID`, `BASE_URL`, `INSECURE=1` (skip TLS verify for local HTTPS).
