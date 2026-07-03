# Test Execution Report — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Quality Engineer · **Stage:** G2
**Executed:** 2026-07-02 · **Mode:** clean · **LLM:** mocked (deterministic)

**Result:** PASS

All three runtime suites are green; every S0001–S0008 acceptance criterion maps to at least
one passing automated test. No fabricated results — commands, counts, and environments below
are reproducible.

## Execution Summary

| Runtime | Command | Result | Environment |
|---|---|---|---|
| Engine (.NET) | `dotnet test tests/Nebula.Tests` | **491 passed / 0 failed / 1 skipped** (~3m44s) | Testcontainers Postgres 16 (Docker) |
| Neuron (Python) | `python3 -m unittest discover -s tests -t .` | **116 passed** (16 test modules) | Python 3.12 (deployed container runtime); canonical `planning-mds/schemas` mounted so `test_schema_drift` resolves |
| Frontend (React) | `pnpm exec vitest run src/features/neuron` | **17 passed** (2 files) · `tsc -b` exit 0 · eslint 0 errors | vitest 2.1.9 + jsdom |

Neuron module breakdown (116): bootstrap 7, registries 7, agent_cards 10, plan_validation 8,
engine_client 5, persistence 10, task_manager 6, schema_drift 2, envelope 6, glance 6,
renewals_head 5, stub_zones 3, actions 8, scope_guard 21, telemetry 9, auth 3.

## Acceptance-Criteria → Test Mapping

### S0001 — Neuron service bootstrap
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| Health/readiness reports healthy + exposes registered heads/tools | `test_bootstrap`, in-container `/health` (4 heads + 5 tools) | ✅ |
| Versioned YAML asset fails fast on unknown head/tool/terminal-state or missing `on_failure` | `test_plan_validation` (8), `test_bootstrap` fail-fast (4) | ✅ |
| Internal A2A-shaped dispatch (no public A2A/Agent Card) | `test_agent_cards` (10), `test_task_manager` (6) | ✅ |
| Durable operation store written; stateless between requests | `test_persistence` (10), `test_task_manager` provenance | ✅ |
| Engine unreachable → typed upstream-unavailable (no 500 leak) | `test_engine_client` (5) | ✅ |

### S0002 — Day-at-a-Glance shell + zone dispatch + envelope
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| One zone slot per registered head (Renewals live + stubs) | FE `DayAtAGlance.test`, `test_glance` | ✅ |
| Heads assembled independently; one failure doesn't blank shell | `test_glance` per-zone isolation | ✅ |
| Versioned multi-part envelope; only registered component ids w/ validated props render (no model markup) | `test_envelope` (6), FE `componentRegistry.test` (6, safe fallback) | ✅ |
| Single auto-titled thread | `test_glance`, FE thread render | ✅ |

### S0003 — Live Renewals zone read
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| Needs-attention list: Identified/Outreach within 90d; row fields | engine `RenewalNeedsAttentionTests`, `test_renewals_head` | ✅ |
| Urgency ordering + 30-day no-contact flag (intra-zone only) | engine `NudgePriorityTests`, `RenewalNeedsAttentionTests` | ✅ |
| Drill context (account, broker, timeline) | engine `RenewalCompanionContextTests` | ✅ |
| Unauthorized rows excluded engine-side; 403 → typed state | engine authorization tests; `test_renewals_head` error isolation | ✅ |
| Empty/typed-error states | `test_renewals_head`, FE empty-state | ✅ |

### S0004 — Stub zones (inert inactive payload)
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| Stub heads return typed `inactive`; no engine read, no CTA | `test_stub_zones` (3) | ✅ |
| All three stubs + live Renewals present | `test_glance`, FE shell | ✅ |

### S0005 — Renewal outreach draft
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| Draft generated (engine-first), editable in-chat, `InternalOnly`, persisted as ActivityTimelineEvent | engine `RenewalOutreachTests`, `test_actions` (draft) | ✅ |
| Content must not state/imply premium/quote/coverage/binding | engine `RenewalOutreachTests` content-constraint asserts | ✅ |
| Provenance event (actor, ts, model, prompt id/version, content hash) | engine `RenewalOutreachTests`, `test_actions` provenance | ✅ |
| Non-`renewal:draft_outreach` → 403, no persist | engine authorization test | ✅ |
| Generation failure → typed error, no partial persist | `test_actions` failure path | ✅ |

### S0006 — Mock-send + workflow transition
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| `Identified → Outreach` transition + WorkflowTransition + "sent (simulated)" event; no real email | engine `WorkflowStateMachineOutreachTests`, `WorkflowEndpointTests`, `test_actions` (mock-send) | ✅ |
| Atomic commit (no "sent" without transition) | engine `WorkflowServiceTests` atomicity | ✅ |
| Invalid transition / unauthorized → rejected, unchanged | engine `WorkflowStateMachineTests`, authorization test | ✅ |
| Idempotent just-in-time If-Match | `test_actions` idempotency | ✅ |

### S0007 — CRM scope guard
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| CRM message routes to matching handler/zone | `test_scope_guard` classify + route, FE composer | ✅ |
| Off-topic → polite CRM redirect (no general-assistant answer) | `test_scope_guard` redirect | ✅ |
| Ambiguous → brief CRM-framed clarify | `test_scope_guard` clarify (input_required) | ✅ |
| Prompt-injection → out-of-scope/denied; guard not bypassed | `test_scope_guard` injection-first priority | ✅ |
| Redirect uses same versioned envelope | `test_scope_guard` envelope, `test_envelope` | ✅ |
| Classifier failure → fail-safe to redirect | `test_scope_guard` fail-safe | ✅ |

### S0008 — Companion telemetry
| Acceptance criterion | Evidence (test) | Verdict |
|---|---|---|
| needs-attention-surfaced (start) + draft-ready (end) primaries | `test_telemetry`, `test_glance` DAU/surfaced, engine `NeuronCompanionTelemetryServiceTests` | ✅ |
| Secondary set (daily-active, % actioned, drafts generated/mock-sent) | `test_telemetry`, engine registry test | ✅ |
| Start event without paired end is measurable (no fabricated end) | `test_telemetry` primaries emitted independently | ✅ |
| Telemetry failure must not break user flow (logged) | `test_telemetry` fire-and-forget-failure test | ✅ |
| user_id must match subject; closed-shape PII boundary | engine `NeuronCompanionTelemetryServiceTests` (403 mismatch), `NeuronCompanionTelemetryEndpointTests` | ✅ |

## Security Scans (QE-run; artifacts under `artifacts/security/`)

QE is responsible for running the scanners; the Security Reviewer owns the verdict at G3.
All scans were run with real tooling on 2026-07-02 (ARM64 host).

| Class | Tool | Result | Artifact |
|---|---|---|---|
| SAST | semgrep (Docker) — 315 rules, `p/python p/typescript p/csharp p/security-audit` | **0 findings** (96 files; all new F0038 files confirmed scanned) | `semgrep-sast.{txt,json}` |
| Secrets | gitleaks 8.30.1 — committed diffs + working tree | **0 leaks** | `secret-scan-gitleaks-history.{txt,json}`, `secret-scan-gitleaks-worktree.txt` |
| Dependency (engine) | `dotnet list package --vulnerable` | **Clean** | `engine-sca-dotnet.txt` |
| Dependency (neuron) | pip-audit 2.10.1 (OSV + PyPI DB) — 27 deployed versions | **No known vulnerabilities** | `neuron-sca-pip-audit.txt` |
| Dependency (frontend) | `pnpm audit --prod` | 9 pre-existing advisories, **not F0038-introduced** (recommend repo waiver) | `frontend-sca-pnpm.txt` |
| DAST | — | Not run this stage — internal service, no new unauthenticated dynamic surface; deferred to platform/CI. See `deployability-check.md` and the G-manifest security-scan handling. | — |

## Notes / Observations (non-blocking; for G3 code review)

- Neuron `app/main.py` (FastAPI route wiring) shows 0% in the unit suite — exercised by the in-container deployability smoke instead. See `coverage-report.md`.
- Frontend `NeuronPanel.tsx` and `useNeuronChat.ts` are at 0% and appear superseded by the S0007 composer flow (`Composer.tsx` + `useSendMessage.ts`) — flag as possible dead code for the code reviewer.

**Result:** PASS
