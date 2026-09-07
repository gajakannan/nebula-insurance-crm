# Historical evidence disposition — 2026-09-07

This is a one-time, repository-scoped disposition for the archived feature
packages reported by R05. It does not change the shared framework contract in
`../nebula-agents/agents/docs/AGENT-OPS.md`, and it does not convert missing
artifacts into passing evidence.

**Disposition owner:** Product Manager / evidence owner

**Supporting roles:** Quality Engineer (test and coverage artifacts), DevOps
(runtime and deployability evidence), Architect (run identity and knowledge
graph reconciliation), Security (security-scan evidence when required), and
the Validate action (cross-artifact verification)

**Approval state:** recorded for follow-up; not accepted as completion proof or
release-readiness approval. The existing V3 approval checkpoint remains
pending.

The local recovery search is documented in
[HISTORICAL-EVIDENCE-RECOVERY-AUDIT.md](HISTORICAL-EVIDENCE-RECOVERY-AUDIT.md).
The one-off contract interpretation is recorded in
[HISTORICAL-CONTRACT-CONCESSION.md](HISTORICAL-CONTRACT-CONCESSION.md).

## Decision

The affected historical packages remain incomplete as historical evidence. No
empty test result, coverage file, screenshot, scratch-path copy, or secret
configuration file will be created to satisfy the validator. Each package must
follow one of these dispositions:

1. **Recover** the original artifact from Git, LFS, CI retention, or an
   approved backup. Preserve its provenance, record its checksum, place it in
   a durable run location, and rerun validation.
2. **Re-enter** the feature when the original artifact cannot be recovered but
   completion proof is still required. Reopen the feature and create a new
   canonical run with a new run ID. An evidence-only rerun references the old
   run with `rerun_of` and has no implementation changes; an implementation
   change requires its own new implementation run. The old run remains
   append-only and is marked historical or superseded by the new closeout.
3. **Record unavailability** when neither recovery nor re-entry is possible.
   The feature remains without current completion proof. The disposition must
   identify the missing artifact, searches performed, owner, approver, date,
   and release impact. This record is an explanation, not a substitute
   artifact.

For this one-off review, the historical `approved` decisions may also be
preserved under the contract concession in
[HISTORICAL-CONTRACT-CONCESSION.md](HISTORICAL-CONTRACT-CONCESSION.md). That
concession does not make the packages pass the current validator and does not
apply to a reopened feature.

## Affected packages

| Feature | Historical run | Validator errors | Required disposition | Current interpretation |
|---|---|---:|---|---|
| F0017 | `2026-06-07-771a5ef6` | 185 | Recover or re-enter | Required completion evidence is unavailable; do not reconstruct the many missing test and coverage outputs with placeholders. |
| F0008 | `2026-07-03-fd732693` | 56 | Recover or re-enter | Missing artifacts, temporary paths, and TRX output require a fresh durable package if this feature is relied upon. |
| F0024 | `2026-07-03-ba011af8` | 19 | Recover or re-enter | Missing KG state, coverage, and scratch health outputs are not accepted as historical proof. |
| F0022 | `2026-07-03-b9f40b31` | 15 | Recover or re-enter | Missing KG state and environment references require provenance recovery or fresh evidence. |
| F0028 | `2026-07-02-736e7854` | 9 | Recover or re-enter | The `.env` reference and missing coverage artifacts cannot be retained as evidence. |
| F0027 | `2026-07-02-b9316621` | 6 | Recover or re-enter | Missing coverage references require recovery or a new coverage run. |
| F0019 | `2026-06-30-6187bd30` | 1 | Re-enter closeout | Add the missing tracker-sync invocation/reference in a new closeout package; do not rewrite the old lifecycle log. |
| F0023 | `2026-06-30-691ec6b8` | 1 | Re-enter closeout | Add the missing tracker-sync invocation/reference in a new closeout package; do not rewrite the old lifecycle log. |
| F0035 | `2026-06-30-bac66bac` | 1 | Re-enter closeout | Add the missing tracker-sync invocation/reference in a new closeout package; do not rewrite the old lifecycle log. |
| F0036 | `2026-06-30-6974ec2c` | 1 | Re-enter closeout | Add the missing tracker-sync invocation/reference in a new closeout package; preserve the superseded run and current feature interpretation. |

The eight governed packages that passed validation remain unchanged. Features
outside the validator's effective-date boundary, and genuinely retired
features, retain their existing skip treatment; that status must remain true in
the registry and `STATUS.md`.

## Evidence rules applied

- `manifest.omissions[]` is not used for required baseline, signoff, closeout,
  coverage-report, or manifest artifacts.
- A coverage waiver may explain missing or below-target detail only when a real
  `coverage-report.md` is present; it cannot replace that report or its raw
  artifact when the contract requires one.
- A security-scan waiver is only for a scan class that genuinely could not run,
  with the required reason, owner, and approval date.
- A validator-defect waiver is not appropriate for an artifact that is simply
  missing. It may be used only when a framework rule is demonstrably defective
  and the required closeout approval names the rule.
- Historical runs are not rewritten to make a later test result appear to have
  been produced at the earlier date.

## Required re-entry record

For every package that is recovered or re-entered, the new evidence package
must include the normal feature-action artifacts, a fresh manifest, durable raw
outputs, an artifact trace, and a closeout decision. The trace must identify
the prior run and every artifact recovered, regenerated, or unavailable. The
feature's `latest-run.json` and `STATUS.md` may be updated only after the new
package has passed the applicable feature-evidence validation.

Until then, R05 remains an evidence-completeness finding and the repository
validation V1 result remains failed. This disposition makes the gap explicit;
it does not claim that the archived checks passed.

## One-off execution order

The evidence owner should run the framework's feature action for each affected
feature as a reopened historical/evidence-re-entry run:

1. **Product Manager:** confirm the feature is genuinely still governed and
   record the re-entry date; do not change a feature to retired or superseded
   solely to suppress validation.
2. **Architect:** bind the new run to the prior run and reconcile any required
   knowledge-graph or plan references.
3. **DevOps and Quality Engineer:** perform runtime preflight, regenerate the
   required test, coverage, and deployability artifacts, and store raw outputs
   under the new run's `artifacts/` tree.
4. **Security:** run required scan classes or record only the contract-supported
   scanner waiver with its reason, owner, and approval date.
5. **Product Manager:** complete tracker synchronization and closeout in the
   new package, then update `latest-run.json` and `STATUS.md`.
6. **Validate action:** run the feature-evidence validator and retain its
   output. A package with unresolved required-artifact errors is not promoted
   as the current completion package.
