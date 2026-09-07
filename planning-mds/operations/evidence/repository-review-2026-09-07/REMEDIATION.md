# Repository-review remediation record

This is the one-off follow-up to the findings in `README.md`. It uses the
framework defect-bugfix action (`D0` through `D5`) and validate action (`V0`
through `V3`) with base-run evidence under
`planning-mds/operations/evidence/runs/2026-09-07-repository-remediation/` and
`2026-09-07-repository-validation/`.

Implemented controls:

- Neuron rejects empty, forged, expired, wrong-issuer, or wrong-audience tokens
  through the engine's authenticated `/internal/identity` endpoint before any
  conversation-store access. The isolated post-fix reproduction returns `401`
  for listing, history, deletion, and empty-bearer creation.
- Staging and production Compose overlays set non-development API environments;
  `deploy.sh` refuses to promote the development base stack without an overlay.
  Custom deploy and rollback commands now honor `--dry-run`.
- Vite proxies every shipped billing, reconciliation, and outbound-document API
  prefix.
- The policy parity gate models the explicit F0020 document-classification
  exception and removes the contradictory BrokerUser account grant.
- Neuron ProblemDetails now includes `code` and `traceId`; `/ready` is documented;
  frontend quality validation enforces ISO timestamps, revision binding, actual
  coverage thresholds, and generated-artifact existence.
- The stale frontend integration fixture, KG test expectations, stale completion
  documents, README commands, and F0040 architecture link were corrected. CI now
  runs frontend, backend, Neuron, KG, policy, API, and frontend-evidence checks.

Post-fix checks: frontend component tests (304), integration tests (22), and
coverage run pass; KG tests pass (187); policy parity passes; the Neuron OpenAPI
validator passes with advisory warnings; and the synthetic forged-token
reproduction returns 401 on every protected operation.

The retained command evidence is in the revision-bound frontend manifest,
`artifacts/kg-tests.log`, `artifacts/openapi-neuron.log`,
`artifacts/dry-run-reproduction.json`, and the post-fix auth reproduction log.

The external framework's registry-wide feature-evidence validator still reports
legacy archived packages with missing historical artifacts. Those packages were
not rewritten or fabricated; the failed validator output remains in the
validation run. The repository-scoped [historical evidence disposition](HISTORICAL-EVIDENCE-DISPOSITION.md)
assigns recovery or evidence re-entry for each affected package. It does not
modify the shared `AGENT-OPS.md` contract or treat missing required artifacts as
accepted evidence. Release readiness requires the affected packages to be
recovered or re-entered, with any genuine framework defect handled through the
framework's own versioned waiver process.

Because the framework evolved while these packages were produced, the review
also records a one-off [historical contract concession](HISTORICAL-CONTRACT-CONCESSION.md).
It preserves the original approved decisions as historical assertions when
recovery is impossible, while retaining the current validator's 294 errors and
3 warnings as a strict current-contract reproducibility limitation. The
concession does not apply to reopened or newly closed features.

Environment limitations remain: Docker/Testcontainers and NuGet restore are not
available in this sandbox. The frontend component, integration, and coverage
suites now pass; the live Playwright visual rerun could not start its configured
web server here, so the retained visual report is explicitly marked as a
sandbox limitation. A bounded Neuron test run passed with 402 tests and 40
skips, but a later full-suite rerun hung in an async test and was interrupted;
the auth reproduction and targeted auth tests remain passing evidence.
