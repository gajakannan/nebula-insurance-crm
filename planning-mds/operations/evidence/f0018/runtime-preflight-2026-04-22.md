# F0018 Runtime Preflight

- Feature: F0018 policy lifecycle and Policy 360
- Run ID: 5ab6f922-bf43-4702-9393-ea8a88c213b8
- Timestamp: 2026-04-22T01:48:14Z
- Gate: G1 RUNTIME PREFLIGHT
- Verdict: PASS

## Commands

- `docker compose up -d --build`
  - Initial result: runtime-blocked because `.env` exported `qAUTHENTIK_SECRET_KEY` instead of `AUTHENTIK_SECRET_KEY`.
  - Repair: corrected local `.env` key name and recreated Authentik containers.
- `docker compose up -d`
  - Result: PASS; `nebula-api` started after `nebula-authentik-server` became healthy.
- `docker compose ps`
  - Result: PASS.
  - `nebula-db`: Up, healthy.
  - `nebula-authentik-server`: Up, healthy.
  - `nebula-authentik-worker`: Up, healthy.
  - `nebula-api`: Up.
  - `nebula-temporal`: Up.
  - `nebula-temporal-ui`: Up.
- `docker compose exec -T authentik-server python -c "import urllib.request; r=urllib.request.urlopen('http://api:8080/healthz', timeout=10); print(r.status); print(r.read().decode()[:200])"`
  - Result: PASS, `200 Healthy`.
- `docker compose exec -T authentik-server python -c "import urllib.request; r=urllib.request.urlopen('http://api:8080/openapi/v1.json', timeout=10); print(r.status); print(r.headers.get('content-type'))"`
  - Result: PASS, `200 application/json;charset=utf-8`.

## Notes

- Host-side `curl http://localhost:8080/...` failed from the current WSL context even while the API was listening inside the container. Runtime validation for this feature will use application runtime containers or the Compose network.
- KG start gate was auto-repaired before G1 by refreshing `planning-mds/knowledge-graph/coverage-report.yaml`; plain `python3 scripts/kg/validate.py` then exited 0.
