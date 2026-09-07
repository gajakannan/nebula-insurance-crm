# One-off historical contract concession — 2026-09-07

## Scope

This concession applies only to the ten archived packages listed in R05 of
the repository review. It is a repository-level interpretation for this
one-off validation; it does not modify `nebula-agents`, the shared validator,
or the evidence contract for future work.

## Rationale

The framework changed while these features were being built. Relevant
framework history includes:

| Framework revision | Date | Change relevant to historical interpretation |
|---|---|---|
| `bd3672c` | 2026-05-23 | Introduced the Phase 1–2b feature-evidence validator and tracker integration. |
| `3137edf` | 2026-05-25 | Added enforced security-scan handoff requirements. |
| `0a1ddb2` / `83eff84` | 2026-05-30 | Added the architect KG gate and renumbered the closeout gates to G5–G8. |
| `dfd7a59` | 2026-05-31 | Consolidated evidence packages into run-ID directories. |
| `689ff71` | 2026-06-01 | Added cold-archive retrieval and `.agentignore` controls. |
| `aed1e7f` / `1e3dc06` | 2026-06-05–06 | Added later G4/G7 validation and contract references. |
| `f83ca30` | 2026-07-02 | Strengthened command-log append and validation behavior. |

The archived manifests carry `contract_effective_date` but do not carry a
framework commit or `contract_version`. The current validator therefore cannot
reconstruct the exact rule set that governed each historical closeout.

## Concession

For this review only:

- The original `status: approved` manifests, gate decisions, signoff records,
  and closeout documents remain accepted as **historical assertions**.
- Missing raw artifacts are recorded as historical reproducibility limitations;
  they do not have to be regenerated solely because later framework revisions
  introduced stricter artifact rules.
- The current validator result remains visible as **294 errors and 3 warnings**.
  It is interpreted as “not reproducible under the current contract,” rather
  than as proof that the original run failed its contemporaneous contract.
- The recovery audit and disposition remain part of the review record. No
  placeholder files, backdated outputs, or copied newer results are accepted.
- Any feature that is reopened, changed, or newly closed must satisfy the
  current framework contract and produce a new canonical run.
- This concession expires for any future review, release train, or re-entry
  unless it is explicitly renewed by the applicable approval authority.

This concession does not make the ten packages current-contract compliant and
does not support a claim that their missing raw artifacts still exist. It allows
the one-off review to preserve their historical completion decisions without
forcing a fabricated retroactive rebuild.

## Approval and ownership

The Product Manager/evidence owner owns the historical disposition. The
framework owner or Architect confirms the contract-evolution rationale, QE
confirms that no raw artifact was fabricated, and the Validate action retains
both the strict validator result and this concession. The user-facing V3
approval record must cite this file if the one-off process validation is
accepted with the concession.
