# Architect Agent Alignment Change Report

**Date:** 2026-06-10  
**Repository:** `nebula-insurance-crm`  
**Harness Source:** `nebula-agents`  
**Purpose:** Review summary for instructor-assigned architecture alignment task

## Executive Summary

This change aligned `nebula-insurance-crm` with the enhanced Architect and Backend Developer guidance in `nebula-agents`.

The CRM remains a .NET-first modular monolith today, but its planning and architecture artifacts now support future decisions for:

- .NET services
- Python services
- modular monolith boundaries
- microservices
- event-driven contracts
- bounded service extraction
- deployment and observability planning

No product feature was implemented. No runtime code was changed.

## What Was Changed

### 1. Polyglot Service Governance

**File:** `planning-mds/architecture/polyglot-service-governance.md`

Added product-local rules for future .NET/Python backend decisions.

This document defines:

- CRM default backend stack as `.NET`
- when Python services are allowed
- required service ownership fields
- required ADR sections for non-default stacks
- prohibited patterns such as shared database ownership or bypassing CRM authorization/audit

**Why it matters:**  
The enhanced `backend-developer` agent determines implementation stack from product architecture guidance. This file gives future agents a clear rule: default to .NET unless an Architect-approved ADR assigns a bounded service to Python.

### 2. Microservices Decision Framework

**File:** `planning-mds/architecture/microservices-decision-framework.md`

Added a decision gate for future service extraction.

This document defines:

- modular monolith as the default
- when microservices are justified
- required evidence before approving a service
- required architecture outputs for distributed services
- extraction modes such as modular module, bounded service, event projection, and integration boundary

**Why it matters:**  
This prevents premature service extraction and matches the `nebula-agents` Architect rule: microservices only when independent deployment, scaling, persistence, runtime, or team-autonomy forces are proven.

### 3. Event Contract Governance

**File:** `planning-mds/architecture/event-contract-governance.md`

Added reusable rules for future event-driven architecture.

This document defines:

- standard event envelope fields
- delivery rules such as transactional outbox, idempotency, replay, and dead-letter handling
- contract format guidance for REST, events, datasets, and internal queries
- audit and security expectations for event payloads

**Why it matters:**  
CRM already has ADR-015 for integration hub concepts. This new document turns those concepts into reusable governance for future bounded services and event-producing features.

### 4. Deployment Topology Guidance

**File:** `planning-mds/architecture/deployment-topology-guidance.md`

Added runtime-planning requirements for future services, workers, relays, and schedulers.

This document defines:

- required topology decisions before runtime implementation
- health and readiness expectations
- observability requirements
- deployment strategy options
- prohibited operational patterns

**Why it matters:**  
The enhanced Architect harness expects service architecture to include deployment, health, observability, and rollback strategy before implementation starts.

### 5. Solution Patterns Alignment

**File:** `planning-mds/architecture/SOLUTION-PATTERNS.md`

Added a new section for polyglot and service-boundary patterns.

The update records:

- `.NET` remains the default backend stack
- CRM is polyglot-ready but not polyglot by default
- service stack assignment is required before code generation
- microservices require ADR-backed justification
- event-driven cross-service changes require outbox or equivalent reliable delivery
- deployment topology must be planned before runtime code

**Why it matters:**  
`SOLUTION-PATTERNS.md` is the product-local source that implementation agents read before coding. This gives the Backend Developer agent enough information to choose the right stack when a future feature is approved for Python.

### 6. Context Routing Alignment

**Files:**

- `planning-mds/context-map.yaml`
- `.agentignore`

Added generic future `services/**` routing as on-demand context.

This does not create a service. It only prepares agent retrieval rules so that if a future `services/{service-name}/` folder exists, agents can load exact service files without broad-loading the entire repo.

**Why it matters:**  
This keeps token usage low and aligns with the `nebula-agents` context-map strategy.

### 7. Planning README Update

**File:** `planning-mds/README.md`

Added pointers to the new architecture governance documents.

**Why it matters:**  
Future agents and reviewers can find the service-governance docs without searching the whole architecture folder.

## What Was Intentionally Not Changed

The following were intentionally not created or modified:

- `engine/**`
- `experience/**`
- `neuron/**`
- APIs
- controllers
- application services
- repositories
- database schemas
- frontend code
- Python service skeletons
- FastAPI services
- Celery workers
- migrations
- feature implementations

## Removed / Not Reintroduced

Earlier exploratory implementation artifacts were removed and were not reintroduced:

- no `F0037-document-processing-service-extraction`
- no `document-processing-service`
- no ADR-025 through ADR-029
- no Python backend runtime code
- no F0020 extraction implementation

F0020 remains only as the already completed and archived product feature.

## Resulting Architecture Capability

After this alignment, `nebula-insurance-crm` can now support future architecture planning for Python services in a controlled way:

1. Architect reviews the feature boundary.
2. Architect decides whether the modular monolith is sufficient.
3. If a service is justified, Architect creates an ADR with stack, data, contract, authorization, audit, observability, and deployment decisions.
4. `SOLUTION-PATTERNS.md` and the feature assembly plan identify the assigned stack.
5. Backend Developer uses the appropriate `.NET` or Python guidance from `nebula-agents`.
6. Runtime code is generated only after architecture approval.

## Validation Performed

The following checks were run after the alignment:

```bash
python3 scripts/validate-context-map.py
python3 -m pytest scripts/tests/test_validate_context_map.py
python3 scripts/kg/validate.py --write-coverage-report
python3 scripts/kg/validate.py
```

All passed. The KG validator still reports an existing low-confidence F0028/F0018 inferred-edge warning, unrelated to this change.

## Files Changed for Architecture Alignment

```text
planning-mds/architecture/polyglot-service-governance.md
planning-mds/architecture/microservices-decision-framework.md
planning-mds/architecture/event-contract-governance.md
planning-mds/architecture/deployment-topology-guidance.md
planning-mds/architecture/SOLUTION-PATTERNS.md
planning-mds/context-map.yaml
planning-mds/README.md
.agentignore
planning-mds/knowledge-graph/coverage-report.yaml
```

## Review Summary

This was an architecture readiness change, not a feature implementation. It made the CRM planning system compatible with the enhanced `nebula-agents` Architect and Backend Developer dual-stack model while preserving the current product runtime and completed feature history.

