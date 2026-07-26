# Knowledge-Graph Reconciliation — F0039 run `2026-07-25-273d5672`

**Gate:** G7 · **Role:** Architect (`agents/architect/SKILL.md` active) · **Date:** 2026-07-25

The graph is reconciled against the **as-built** source — what shipped, not what G0
predicted — so the architect who reads `lookup.py` at the *next* feature's G0 gets the
truth.

## Binding Delta

Measured against the G0 declaration.

The G0 Knowledge-Graph Binding Plan predicted 7 new capabilities, the `neuron-thread-*`
endpoints, and 3 schemas, with `code-index.yaml` bindings deliberately deferred because no
implementation existed at plan time. That deferral is now settled.

**Authored in `kg-source/bindings/node_bindings.yaml` (6 new binding shards):**

| Capability | Bound surface |
|------------|---------------|
| `capability:neuron-conversation-store` | `neuron/app/persistence/**`, migrations, its two test files |
| `capability:neuron-thread-management` | `neuron/app/threads.py` + the 4 `experience/` conversation files |
| `capability:neuron-structured-model-provider` | `neuron/app/models/**`, `config/models.yaml` |
| `capability:neuron-intent-catalog` | `intent/{catalog,contracts,prompt_registry}.py`, `config/intent-catalog.yaml`, `prompts/**` |
| `capability:neuron-intent-resolution` | `intent/{preflight,resolver,response_policy,validation}.py`, `messages.py` |
| `capability:neuron-intent-evaluation` | `intent/{evaluation,evaluate_cli}.py`, `evals/**` |

Bound by **directory glob** where a folder is one cohesive capability, per the gate rule.

**Coverage confirmed, not duplicated:** `capability:neuron-companion` already globs
`neuron/app/**`, `neuron/crm_agents/**`, and `neuron/tests/**`, so every new file was
already covered. The new shards *narrow* each capability to the surface that actually
implements it rather than re-declaring what the broad glob catches.

**`capability:neuron-intent-adjudicator` is intentionally unbound** — S0009 is
gated-deferred and has no implementation. It remains a declared canonical node with no
code binding, which is the accurate state.

## Canonical nodes

**None introduced at this gate.** The 7 capabilities, 6 `neuron-thread-*` endpoints, 3
schemas, and `adr:035` were authored as node shards during the plan run and compiled then.
G7 added bindings only — the as-built source matched the predicted semantic surface, so no
node shard needed adding or amending.

## Validator Results

`knowledge-graph/*.yaml` was **never hand-edited**. All regeneration ran through the tools:

| Command | Result |
|---------|--------|
| `scripts/kg/compile.py` | wrote `canonical-nodes.yaml`, `feature-mappings.yaml`, `code-index.yaml`, `solution-ontology.yaml` + REGISTRY/ROADMAP regions |
| `validate.py --regenerate-symbols --check-symbols --regenerate-decisions --check-decisions` | **exit 0 — PASS** |
| `validate.py --check-drift` | **exit 0 — PASS** |

`--write-coverage-report` was **deliberately not run here**: it binds feature-doc paths
that the G8 archive move relocates, so regenerating now would immediately re-stale it.
Deferred to G8 after the move, per the gate constraint.

## Warnings, and one I caused

`--check-drift` passes with warnings. Three are attributable to this run:

> `symbol:capability-neuron-companion:{agent-registry.get,envelope.text-part,tool-registry.get}.callers`
> reference unknown symbol `…message-dispatcher.route-in-scope`

**Cause:** S0007 renamed `MessageDispatcher._route_in_scope` to `_route_head` (it now
serves both the direct and deterministic paths). The symbol regenerator re-derived the
defined symbols but left the inbound `callers` edges pointing at the old name — it prunes
definitions, not stale references to them.

**Assessment:** cosmetic and non-blocking — the integrity check passes and no binding,
node, or coverage claim depends on those edges. Recorded rather than silently accepted, and
carried as a follow-up so the next run does not inherit an unexplained warning. It is a
regenerator gap, not a graph inconsistency.

The remaining warnings are pre-existing and unrelated to F0039 (a low-confidence F0018→
F0028 inferred edge; a `distribution_rollup` Casbin pair with no policy_rule node; and
binding-overlap notices on `planning-mds/security/**` paths claimed by many nodes).

## Handoff to Closeout

`--write-coverage-report` is deliberately **not** run at this gate and is handed to G8, to be run only after the
archive move relocates feature-doc paths. Closeout **verifies** this graph; it does not re-author shards. If a
binding gap were found at G8 it routes back here for a G7 delta pass rather than being patched in closeout.

## Completion criteria

- [x] Architect role active for the reconciliation
- [x] `code-index.yaml` bindings exist for every new capability surface (glob-based; existing coverage confirmed, not duplicated)
- [x] Canonical nodes: **none introduced** — stated explicitly
- [x] `--regenerate-symbols --check-symbols --regenerate-decisions --check-decisions` exit 0
- [x] `--check-drift` exit 0
- [x] `coverage-report.yaml` **not** regenerated at this gate (deferred to G8, post-move)
- [x] `kg-reconciliation.md` written
