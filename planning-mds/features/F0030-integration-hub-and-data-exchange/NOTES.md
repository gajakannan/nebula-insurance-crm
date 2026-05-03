# F0030 — Notes & Deferred Considerations

Captured for review when F0030 is scheduled. The product-neutral contract-governed exchange pattern is now reflected in the active PRD; the specific ODCS adoption, tooling, and first contract files remain deferred decisions.

## Open Data Contract Standard (ODCS) for external data exchange

### Idea

Adopt ODCS v3.x as the contract format for **dataset-shaped** exchanges flowing through the integration hub — imports, exports, batch feeds, snapshot extracts. ODCS would sit one layer outward from Nebula's existing API contracts, governing the shape, quality, freshness, lineage, and access terms of data crossing the system boundary.

### Placement

```
Nebula Core Domain
  Account / Broker / Submission / Policy / Renewal / Document
        |
        v
Canonical integration models + mapping layer  (this feature, F0030)
        |
        v
Integration Hub
  outbox, inbound landing zone, connector adapters, replay, monitoring
        |
        v
ODCS YAML contracts
  schema, quality, freshness, lineage, server/location, access terms
        |
        v
External systems
  finance, carriers, document systems, analytics warehouse, batch imports
```

### What ODCS would govern

- **F0031 imports:** broker, account, contact, policy, submission import files landed as traceable exchange records and validated against versioned contracts before deduplication review or promotion.
- **Outbound data products:** `policy-export-v1`, `broker-account-export-v1`, `activity-timeline-export-v1`, etc. — required identifiers, classifications, freshness, row-count expectations, ownership.
- **Connector acceptance gates:** a connector declares which contract version it produces or consumes; exchanges validate against that contract before being marked successful.
- **Knowledge-graph alignment:** add a `data_contract` node type alongside the existing `api_contract` so the solution ontology treats external contracts as first-class.

### What ODCS would not replace

- `planning-mds/api/nebula-api.yaml` remains the authoritative REST contract.
- JSON Schema remains the validation surface for frontend/backend DTOs.
- Pact and Bruno remain the consumer/provider behavioral checks for application APIs.
- ODCS is not the core domain model and not the EF migration source.
- ODCS server blocks are not a place to store real secrets.

### Caveats worth surfacing at decision time

- The older "Data Contract Specification" is deprecated; ODCS (under Bitol / LF AI & Data) is the live standard. Author against ODCS v3.x. The Data Contract CLI still works as tooling.
- ODCS is dataset-oriented. For event-style outbox payloads (e.g. `policy.bound.v1`), AsyncAPI may be a better fit than ODCS. Recommend scoping the first wave to imports + snapshot exports and explicitly deferring the event-contract question.
- ADR-015 already flags canonical-contract versioning as a known risk; ODCS gives that discipline a concrete shape but does not remove the operational burden.

### Suggested first step (when work begins)

Start with lint-only CI — no enforcement against live data:

```
planning-mds/data-contracts/
  broker-import-v1.odcs.yaml
  account-import-v1.odcs.yaml
  policy-export-v1.odcs.yaml
  activity-timeline-export-v1.odcs.yaml
```

Wire a `data_contracts` validation gate alongside the existing `knowledge_graph_sync` and `solution_contract` gates. Promote to active enforcement only once F0030/F0031 implementation lands.

### Decision status

The general contract-governed landing-zone pattern is now part of the F0030 PRD. Specific ODCS adoption remains **not decided**. Revisit when ADR-015 follow-up "Define first-wave canonical contracts and replay controls" is picked up.

### References

- ADR-015 — Integration Hub, Canonical Contracts, and Outbox
- F0031 — Data Import, Deduplication & Go-Live Migration
- Open Data Contract Standard: https://bitol-io.github.io/open-data-contract-standard/latest/
- Data Contract CLI: https://cli.datacontract.com/
- Data Contract Specification (deprecated): https://datacontract-specification.com/
