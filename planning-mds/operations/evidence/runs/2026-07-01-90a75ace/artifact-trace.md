# Artifact Trace — F0038-neuron-day-at-a-glance-shell run 2026-07-01-90a75ace

> Captures what was read, written, generated, referenced externally, and explicitly omitted/waived.

## Artifacts Read

- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/PRD.md`
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/intake-brief.md`
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/F0038-S0001..S0008-*.md` (8 stories)
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/STATUS.md`
- `planning-mds/architecture/SOLUTION-PATTERNS.md`
- `planning-mds/architecture/decisions/ADR-027-*.md`, `ADR-028-*.md`
- `planning-mds/BLUEPRINT.md` §3 (Neuron Companion) + §4.7
- `planning-mds/api/*` Neuron + engine contracts for F0038
- KG: `solution-ontology.yaml`, `canonical-nodes.yaml`, `feature-mappings.yaml`, `code-index.yaml`, `coverage-report.yaml`

## Artifacts Created Or Updated

- `evidence-manifest.json` — created (draft → in-progress); G2: booleans reconciled to
  as-built (frontend_in_scope, deployment_config_changed, security_sensitive_scope = true),
  gate_results.self_review + .deployability, role_results QE + DevOps, security_scans populated.
- `README.md`, `action-context.md`, `artifact-trace.md`, `gate-decisions.md` — created
- `commands.log`, `lifecycle-gates.log` — initialized empty
- G0: `g0-assembly-plan-validation.md`. G1: `g1-runtime-preflight.md`.
- G2: `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`,
  `deployability-check.md`.
- G3: `code-review-report.md` (APPROVED WITH RECOMMENDATIONS), `security-review-report.md`
  (PASS); manifest role_results += Code Reviewer + Security Reviewer.

## Generated Evidence

Tool-produced outputs recorded under `artifacts/` as gates run:

- `artifacts/diffs/changed-files.txt` — branch delta (committed + working tree).
- `artifacts/coverage/` — `engine-coverage.cobertura.xml`, `neuron-coverage.xml` (+report),
  `frontend-coverage.cobertura.xml` (+report).
- `artifacts/security/` — `semgrep-sast.{txt,json}` (SAST), `secret-scan-gitleaks-history.{txt,json}`
  + `secret-scan-gitleaks-worktree.txt` (secrets), `engine-sca-dotnet.txt` +
  `neuron-sca-pip-audit.txt` (+`neuron-audited-requirements.txt`) + `frontend-sca-pnpm.txt` +
  `dependency-sca-summary.md` (dependency SCA).

## External Or Global Evidence References

- (to be populated if any global lane evidence is linked)

## Omissions And Waivers

Mirror manifest `omissions[]` / `waivers`.

- **security_scans.dast** — waived at G2. Reason: Neuron and the engine telemetry endpoint
  are internal, authenticated services; F0038 adds no new unauthenticated dynamic attack
  surface (`/v1/*` → 401 without a forwarded token; ingest requires authorization). DAST
  (OWASP ZAP) is a platform/CI concern against a deployed environment. SAST, secret-scanning,
  and dependency SCA ran clean this stage. Owner: Quality Engineer (run 2026-07-01-90a75ace),
  Security Reviewer to ratify at G3. Approved_on: 2026-07-02.

## Run Environment (conditional)

Required only when `commands.log` carries an absolute `cwd`. To be populated if any command
records an absolute cwd.
