# F0034 G0/G1 Evidence

**Date:** 2026-05-07
**Run ID:** 913f3e51-9bb5-4af9-80ba-d7778abbd304

## G0 - Architect Assembly Plan Validation

- `feature-assembly-plan.md` exists under the F0034 feature folder.
- User approved the Phase B assembly plan on 2026-05-07.
- Workstate decision topics recorded:
  - `phase-b-approval`
  - `f0034-assembly-plan-created`
- Required signoff roles remain initialized in `STATUS.md`: Quality Engineer, Code Reviewer, Security Reviewer, DevOps, Architect.

## G1 - Runtime Preflight

Commands executed:

```text
docker compose ps
```

Result summary:

- `nebula-db`: Up, healthy.
- `nebula-authentik-server`: Up, healthy.
- `nebula-authentik-worker`: Up, healthy.
- `nebula-api`: Up.
- `nebula-temporal`: Up.
- `nebula-temporal-ui`: Up.

Host curl to `http://localhost:8080/healthz` returned connection refused, but the API runtime container itself accepted the health probe:

```text
docker compose exec -T api bash -lc "exec 3<>/dev/tcp/127.0.0.1/8080; printf 'GET /healthz HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3; head -c 200 <&3"
```

Result summary:

```text
HTTP/1.1 200 OK
Content-Type: text/plain
Server: Kestrel
```

KG validation:

```text
python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --write-coverage-report
python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py
```

Result summary:

- Knowledge graph validation passed.
- Warning remains: low-confidence inferred edge on `feature:F0028` in `feature:F0018.depends_on`; unrelated to F0034.
