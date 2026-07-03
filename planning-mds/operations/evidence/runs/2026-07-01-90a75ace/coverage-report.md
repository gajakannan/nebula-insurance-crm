# Coverage Report — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Quality Engineer · **Stage:** G2 · **Recorded:** 2026-07-02

Coverage was captured for all three runtimes. No coverage waiver is claimed — measured
coverage on the F0038 surface is healthy at every tier.

| Runtime | Statements / Lines | Branch | How measured | Artifact |
|---|---|---|---|---|
| Engine (.NET) | Cobertura from the 491-test run | — | `dotnet test` + Coverlet (Testcontainers Postgres) | `artifacts/coverage/engine-coverage.cobertura.xml` |
| Neuron (Python) | **88%** (1221 stmts, 134 miss) | 224 branches / 28 partial | `coverage run --branch --source=app -m unittest` in `python:3.12-slim` (schemas mounted); full 116-test suite green | `artifacts/coverage/neuron-coverage.xml` (plus `neuron-coverage-report.txt`) |
| Frontend (React) | **86.2%** | 76.7% | `vitest run --coverage` (v8), scoped to `src/features/neuron` | `artifacts/coverage/frontend-coverage.cobertura.xml` (plus `frontend-coverage-report.txt`) |

## Neuron — per-module highlights

Well-covered core: `engine_client.py` 100%, `runtime.py` 100%, `registries.py` 100%,
`persistence/models.py` 100%, `glance.py` 98%, `scope_guard.py` 96%, `actions.py` 96%,
`telemetry.py` 95%, `zone_heads.py` 95%, `messages.py` 93%.

**Known gap:** `app/main.py` = 0%. This is the FastAPI ASGI wiring (route declarations +
app construction). It is exercised by the in-container deployability smoke (`/health`,
`/ready`, `/v1/glance` 401, `POST /v1/messages` redirect — see `deployability-check.md`),
not by the unit suite, which targets pure logic. Not a correctness gap; the routes are
proven live.

## Frontend — per-file highlights

Active F0038 code is 88–100%: `DayAtAGlance.tsx` 88.9%, `Composer.tsx` 93%,
`useSendMessage.ts` 100%, `useCompanionAction.ts` 100%, `SessionContext.tsx` 98%,
`AttentionList.tsx` 97%, `ZoneSlot.tsx` 98%, `componentRegistry.tsx` 100%, `constants.ts` 100%.

**0% entries (not correctness gaps):**
- `types.ts` — type-only module, no runtime.
- `index.ts` — barrel re-export.
- `NeuronPanel.tsx`, `useNeuronChat.ts` — appear **superseded by the S0007 composer flow**
  (`Composer.tsx` + `useSendMessage.ts`). Flagged for the G3 code reviewer as candidate dead
  code; if confirmed, removal (not new tests) is the correct resolution.

## Reproduction

- Neuron: `docker run --rm -v <neuron>:/work -v <repo>/planning-mds/schemas:/planning-mds/schemas:ro -w /work python:3.12-slim sh -c 'pip install -e . coverage && coverage run --branch --source=app -m unittest discover -s tests -t . && coverage xml'`
- Frontend: `pnpm exec vitest run --coverage --coverage.include='src/features/neuron/**' src/features/neuron`
