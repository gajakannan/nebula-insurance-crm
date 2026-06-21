# F0023 Acceptance Criteria Checklist

Use this checklist while reviewing F0023 stories and implementation evidence.

## Clarity And Testability

- [ ] Each story has at least one happy path, one edge/error case, and one authorization expectation.
- [ ] Object types in scope are explicit: broker, MGA/program context, account, policy, submission, renewal, and task.
- [ ] Acceptance criteria distinguish empty results, unauthorized/no-access outcomes, stale results, and system errors.
- [ ] Saved-view criteria name create, rename, update, delete, apply, reload, and permission behavior.
- [ ] Operational-report criteria name the report dimensions, drilldown behavior, and source-record navigation.

## Search And Navigation

- [ ] Global search supports grouped results by object type.
- [ ] Result rows include enough context to disambiguate similar records: identifier, display name, status, owner, and source object type where available.
- [ ] Search criteria are deep-linkable and survive browser reload.
- [ ] Opening a result navigates to the authoritative source screen.

## Saved Views

- [ ] Personal saved views persist after reload for the owner.
- [ ] Team saved views are visible only to eligible team members.
- [ ] Saved views never grant access to records outside the user's authorization scope.
- [ ] Shared default view changes are audited.

## Operational Reports

- [ ] Due-work, workload by owner/status, and workflow aging report rows drill down to filtered record lists.
- [ ] Reports use source workflow/task fields already owned by upstream features.
- [ ] Hierarchy/territory dimensions degrade when F0017 data is not available.
- [ ] F0037-owned hierarchy-aware access rollups are not implemented under F0023.

## Security And Audit

- [ ] Authorization is enforced before returning result rows, snippets, suggestions, counts, or saved-view previews.
- [ ] Unauthorized records are omitted without leaking hidden object existence.
- [ ] Saved-view mutations create audit/timeline evidence or equivalent immutable administrative audit.
- [ ] System errors use user-safe messages and preserve existing criteria for retry.

## Out Of Scope

- [ ] No external broker/MGA search or reporting.
- [ ] No free-form report builder or SQL-like query tool.
- [ ] No advanced BI/predictive analytics.
- [ ] No full document-content search.
- [ ] No hierarchy-aware access-control enforcement or distribution rollup authorization.
