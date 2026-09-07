# CRM plan review checklist

The local check validates evidence-package durability. Reviewers still own
scope, architecture, security, buildability, and readiness judgments.

| Rule ID | Owner | Criterion | Governing source | Planning evidence to inspect |
|---|---|---|---|---|
| CRM-EVIDENCE-DURABILITY | product-manager | New evidence packages retain contract identity, durable files, and resolving artifact references. | planning-mds/operations/evidence/README.md | Changed run manifests, commands.log, lifecycle-gates.log, and artifact tree |

Historical packages before the recorded compatibility boundary are dispositioned
by `planning-mds/operations/evidence/repository-review-2026-09-07/` and remain
historical evidence. The local check must not backfill or reinterpret them.
