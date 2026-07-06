# DAST Scan Summary — F0027 run 2026-07-02-b9316621

Command:

```text
sh agents/security/scripts/run-dast-scan.sh --target http://localhost:8080 --max-minutes 1 --report-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-02-b9316621/artifacts/security --ignore-warn
```

## Result

NOT COMPLETED.

## Reason

The first sandboxed attempt failed because Docker socket access was denied. The escalated rerun reached Docker but failed because image `owasp/zap2docker-stable:latest` could not be pulled/found. No ZAP report was produced.
