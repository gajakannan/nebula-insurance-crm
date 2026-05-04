# ADR-019: Mock-quarantine-then-promote Ingest Pipeline for Documents

**Status:** Accepted
**Date:** 2026-05-04
**Owners:** Architect
**Related Features:** F0020
**Related ADRs:** ADR-012 (shared document architecture)

## Context

ADR-012 mandates a scanning hook in the document subsystem. MVP cannot integrate a real malware scanner, but the absence of a scanner cannot become a back door — we must ship the structural pipeline so that swapping in a real scanner is an interface swap, not a redesign.

The user-clarified MVP design (G1, F0020 plan run, 2026-05-04) is a 60-second timer that holds every uploaded binary in a quarantine folder before promoting it to its target location. This ADR records the pipeline contract and the scanner-replacement interface so a real scanner can be wired in later without changing feature code or read paths.

## Decision

The document ingest pipeline has two stages:

### Stage 1 — Quarantine on upload

1. Every accepted upload (single, bulk, replace, template materialise) writes the binary to `<docroot>/quarantine/{upload-id}` and creates or updates the parent's sidecar JSON with a new `versions[N]` entry whose `status = quarantined`.
2. The upload response is HTTP 202 Accepted and surfaces `documentId` plus the quarantined version `n`. The document **does not** appear on the parent's documents list (S0004) or detail (S0005), and is **not** downloadable (S0006), until promotion completes.
3. The quarantine folder is not user-readable; only the upload service and the worker may write to it.

### Stage 2 — Promote after the configured hold

1. An idempotent worker polls (default tick 10 s; bound 5-30 s) and processes any `quarantined` entry whose `uploadedAt` exceeds the configured hold (default 60 s; bound 30-300 s; configured via `<docroot>/configuration/document-retention-policies.yaml#quarantine.holdSeconds`).
2. Promotion is **atomic**: rename the binary into `<docroot>/{parent-type}/{parent-id}/{logical}-v{N}.{ext}` (or copy-then-delete with verified SHA-256 if the rename crosses a filesystem boundary), then flip the sidecar JSON `versions[N-1].status` to `available`, append an `events: [{kind: "promoted", at, byUserId: "system:quarantine-worker", version: N}]` row, and emit one `ActivityTimelineEvent` of type `DocumentPromoted` per SOLUTION-PATTERNS §2.
3. Idempotent semantics: re-running the worker on an already-`available` entry is a no-op (no event row, no error).
4. Failure semantics: a promotion failure appends `{kind: "promote_failed", at, error}` to the sidecar `events[]` and leaves the binary in quarantine. After 5 consecutive failures the worker flips the status to `failed_promote` and stops retrying. Operators remediate via the retention sweeper (S0011) or a future admin UI; nothing in MVP auto-recovers.
5. The worker holds the per-document lock (shared with replace S0007 and metadata-edit S0008) while flipping status to avoid racy event ordering.

### Scanner replacement contract

A real scanner integration must satisfy this interface:

```
interface IQuarantineScanner {
  // Called by the worker before promotion; must be idempotent.
  // Returns ScanResult ∈ { Clean, Infected(reason), Inconclusive(reason) }.
  // Must complete within `holdSeconds * 5` or it is treated as Inconclusive.
  ScanResult Scan(QuarantineEntry entry);
}
```

- `Clean` → worker promotes as described.
- `Infected` → worker leaves the binary in quarantine, appends `{kind: "scan_infected", reason}` to sidecar `events[]`, sets `versions[N-1].status = failed_promote`, emits an alert.
- `Inconclusive` → worker leaves the binary in quarantine, appends `{kind: "scan_inconclusive", reason}`, retains the entry for the next tick (subject to the 5-failure cap).

The MVP `MockTimerScanner` returns `Clean` after the hold elapses — no actual scanning. Replacing it is configuration-only.

### Configuration

`<docroot>/configuration/document-retention-policies.yaml` carries the quarantine knobs alongside retention:

```yaml
version: 1
defaultRetentionDays: 10
perType: { acord: 10, loss-run: 7, financials: 7, supplemental: 3, template: 10 }
quarantine:
  holdSeconds: 60          # 30-300
  workerTickSeconds: 10    # 5-30
  maxRetries: 5
```

Out-of-range values fail the loader; prior policy remains in force.

### Read-path coupling

List (S0004) and Detail (S0005) read the sidecar JSON and surface `versions[N-1].status`. They must not surface a `quarantined` or `failed_promote` version as if it were `available` — both expose the badge instead of action buttons. Download (S0006) returns HTTP 409 `{code: "version_not_available"}` for non-`available` requests.

### Audit pairing

For every promote, two audit records are produced:
- The granular sidecar JSON `events[]` row (single source of truth for document-level history).
- One `ActivityTimelineEvent` row per SOLUTION-PATTERNS §2 with type `DocumentPromoted` and the document/parent IDs in the payload.

This dual-record pattern is necessary because the canonical timeline drives the cross-feature feed; sidecar `events[]` drives the per-document detail view.

## Consequences

### Positive

- The pipeline is testable: the mock satisfies the same interface a real scanner will, and integration tests assert the contract independently of the scanner internals.
- No back door — every document, including replaced versions and materialised templates, passes through quarantine.
- Failure modes are observable and bounded.
- A real scanner integration is a single replacement, not a redesign.

### Negative

- 60 seconds is annoying for demos and tests. Tests can override `quarantine.holdSeconds` per environment (bounded to 30 s minimum), so CI runs are bounded without weakening the MVP demo behaviour.
- The worker must hold the per-document lock briefly during status flip; coordination with replace/metadata-edit is required.
- A failed_promote document remains visible (with badge) until retention sweeps it; this is acceptable but the badge must be unmistakable.

## Implementation Notes

- The worker runs as a hosted service in the engine process for MVP; a future move to a dedicated process is a deployment change (not a contract change).
- `IQuarantineScanner` lives in the application layer; the `MockTimerScanner` implementation lives in infrastructure.
- The lock primitive (per-document) is already required by S0007 (replace) and S0008 (metadata-edit); the worker reuses the same primitive.

## Follow-up

- F0020-S0003 implements the pipeline.
- F0020-S0011 owns retention-side coordination and consumes the same configuration file.
- A future ADR will record the real-scanner choice when MVP is replaced.
