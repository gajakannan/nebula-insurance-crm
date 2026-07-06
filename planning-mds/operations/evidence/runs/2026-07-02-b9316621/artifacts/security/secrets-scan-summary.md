# Secrets Scan Summary — F0027 run 2026-07-02-b9316621

Command:

```text
sh agents/security/scripts/check-secrets.sh --path /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --report-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-02-b9316621/artifacts/security
```

## Result

NOT RUN.

## Reason

The wrapper exited with setup error because `gitleaks` is not installed in `PATH`, and no `SECRET_SCAN_CMD` override is configured.
