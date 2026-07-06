# F0017 Sample Example

Run: `2026-06-07-771a5ef6`
Feature: `F0017-broker-mga-hierarchy-and-producer-ownership`
Verified on: `2026-07-03`

This sample uses the local development broker `Anchor Advisors 015`.

## Open The Example

1. Start the local API and frontend.
2. Open `http://127.0.0.1:5173/brokers/e2bb173c-ae3c-431b-bcd6-98f21f04448c`.
3. Select the `Distribution` tab.

## Expected Distribution Output

### Hierarchy

The hierarchy breadcrumb should show:

```text
(root) > Acme MGA > Anchor Advisors 015
```

The children list should show:

```text
Producer  Anchor Advisors 015 Producer A  0 children
Producer  Anchor Advisors 015 Producer B  0 children
```

### Ownership

The ownership panel should show:

```text
Owner: Anchor Advisors 015 Producer A
Effective 2026-01-01 -> open
```

### Territories

The current territory panel should show:

```text
Assigned to F0017 Demo - Southeast
Effective 2026-04-01 -> open
```

Use the territory `As of` date input to verify effective-dated reassignment:

```text
2026-02-01 -> F0017 Demo - Northeast
2026-05-01 -> F0017 Demo - Southeast
```

This proves that the prior open assignment was closed when the new territory assignment became effective.

## Expected Timeline Output

Select the `Timeline` tab. The activity list should include:

```text
Territory reassigned from F0017 Demo - Northeast to F0017 Demo - Southeast effective 2026-04-01
Dev Seed

Producer ownership assigned to e2bb173c-ae3c-431b-bcd6-98f21f041701 effective 2026-01-01
Dev Seed
```

## Verified Checks

The live UI was verified with Playwright against `http://127.0.0.1:5173`:

- Distribution breadcrumb rendered.
- Hierarchy children rendered.
- Ownership rendered.
- Current territory rendered.
- Territory as-of lookup returned the prior and current territories.
- Timeline rendered ownership and territory reassignment events.
- Actor context rendered as `Dev Seed`.

## Portability Notes

Another contributor should see the same example after:

1. Pulling this implementation.
2. Running database migrations.
3. Starting `Nebula.Api` once so idempotent development seed repair runs.
4. Starting the Vite frontend with local development auth/proxy configuration.

The sample rows are created idempotently by development seed repair. They do not require a new migration and do not implement deferred F0037 authorization or rollup behavior.
