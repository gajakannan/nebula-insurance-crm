# F0036 Action Context

## Run Identity

Feature: F0036
Slug: dynamic-product-attribute-form-engine
Run ID: 2026-06-30-6974ec2c
Prior run remediated: 2026-05-28-077b7b30
Run type: remediation/revalidation

## Inputs

- Current archived tracker: `planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/STATUS.md`
- Prior evidence package: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/`
- Current product code under `experience/src/features/lob-attributes` and `experience/src/features/forms`
- Current framework validators in `../nebula-agents/agents/product-manager/scripts/`

## Assumptions

The remediation run validates the current workspace implementation and evidence contract. It does not assert that the original 2026-05-28 closeout commit was rebuilt or retested. The historical STATUS signoff rows already use bare artifact filenames, so the new run can become authoritative without modifying archived signoff history.

## Scope Boundaries

In scope: create a durable run-local evidence package, capture missing test/coverage/security artifacts, use repo-relative artifact references, promote `latest-run.json`, and mark the prior manifest superseded through the framework helper.

Out of scope: changing F0036 product behavior, regenerating historical old-run artifacts, re-running against the original closeout commit, or repairing unrelated KG coverage drift.

## Lifecycle Stage

This run is a G6/G8 remediation closeout package for an already archived feature.
