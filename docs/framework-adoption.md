# Framework adoption

CRM opts into the product-owned instruction and check contract through
`.nebula-project.yaml`. The framework pin is
`c218bf1776f509a30f71967a1ee79879caa9a000`; the workflow pin and
`planning-mds/BLUEPRINT.md` binding must advance together.

The declared `evidence-durability` check runs at the framework's supported
`plan-review` PR2 extension point. It invokes
`scripts/validation/validate_evidence_durability.py` through
`agents/scripts/project_checks.py`, so the framework records the resolved
command, input hashes, structured result, and stdout/stderr under the base run.
The check is additive and does not replace framework validators or approvals.

The check delegates to `scripts/validate-evidence-package.py`. That guard has
one explicit historical boundary: packages recorded before `2026-09-07` are
skipped under the accepted repository-review concession. Packages at or after
that boundary must satisfy the current durability contract. No historical
placeholder or backdated evidence is generated.

`scripts/run-lifecycle-gates.py` is a separate product-owned runner for the
gates in `lifecycle-stage.yaml`. It runs from the CRM repository root and is
invoked by the product CI job. It is not a native host hook and does not alter
the framework's action approvals.

For local framework sessions, use an explicit product root:

```bash
cd ../nebula-agents
python3 agents/scripts/project_context.py \
  --product-root ../nebula-insurance-crm --action plan-review
```
