# Action Context

> plan action (Phase A + B), base-run-only. Contract 2026-07-11.

## Run Identity

- **action:** plan
- **contract_effective_date:** 2026-07-11
- **contract_version:** 2026-07-11
- **feature_id:** F0039
- **feature_index_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0039-neuron-multi-thread-conversations (NOT created at plan — feature action owns it)
- **feature_slug:** neuron-multi-thread-conversations
- **mode:** clean
- **product_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm
- **run_folder:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-21-6eeb172f
- **run_id:** 2026-07-21-6eeb172f
- **run_id_prior:** None

## Inputs

- **FEATURE_ID:** F0039
- **PHASE:** A+B (Phase A requirements, then Phase B architecture)
- **FEATURE_MODE:** existing (F0039 already has a provisional skeleton — update, don't scaffold-new)
- **Primary design source:** `planning-mds/features/F0039-neuron-multi-thread-conversations/neuron-phi-intent-security-implementation-spec.md`
  ("Neuron Durable Conversations and Local Phi Intent Resolution", v1.1.0, 2026-07-21, 4270 lines,
  local Phi runtime verified 2026-07-21). This spec is the raw architecture/design input distilled here
  into PRD + stories (Phase A) and feature-assembly-plan + ADR + ontology bindings (Phase B).

## Assumptions

- F0038 (Neuron Day-at-a-Glance Shell) is Done & archived (2026-07-02, run 2026-07-01-90a75ace); its
  persistence-home ADR (ADR-028), message envelope, scope-guard seam, and `neuron.*` schema scaffold
  (`0001_neuron_schema.sql`) are the durable dependency this feature builds on.
- ADR-028 is authoritative: **Neuron owns and writes `neuron.*` directly** (Postgres via the engine DB, not
  through the engine API). Any provisional PRD wording that says persistence is "written through the engine"
  is corrected in this run (spec §2.7).
- The AI Engineer role (framework) is available and owns the Phi provider + evaluation harness.

## Operator clarifications (G1 inputs — resolved 2026-07-21)

1. **Story scope:** F0039 = all 9 stories S0001–S0009 exactly per spec §37. S0009 (Phi contextual
   adjudicator) is committed but **GATED** — implemented only after S0001–S0008 direct-routing + context
   gates pass (rollout Phase 4).
2. **Display name:** updated to **"Neuron Durable Conversations & Local Phi Intent Resolution"** in
   REGISTRY / ROADMAP / PRD. Folder slug `neuron-multi-thread-conversations` unchanged (baked into this run).
3. **Required signoff roles:** Quality Engineer, Code Reviewer, Architect, **AI Engineer**, **Security**.

## Scope Boundaries

- **In scope (this plan run):** Phase A (PRD, 9 stories, STATUS/README/GETTING-STARTED, acceptance-criteria
  checklist, feature-mappings stub, REGISTRY/ROADMAP sync) and Phase B (feature-assembly-plan, ADR for the
  Phi intent-resolution layer, `neuron-api.yaml` thread/history contract, completed feature-mappings +
  canonical-node bindings, ontology sync).
- **Out of scope:** any implementation/code (feature/build actions); the feature evidence package at
  FEATURE_INDEX_ROOT (feature action owns it); a second live specialist head (F0040); open-ended response
  composer (a subsequent feature); thread sharing / cross-user visibility / full-text thread search (Later).

## Lifecycle Stage

- plan run initialized; Phase A drafting.
