# planning-mds (Solution-Specific)

This directory contains **all solution-specific requirements and references**. It should be created from scratch for each new project.

**Note:** The current content (domain/, examples/, features/, screens/, etc.) is for **Nebula CRM** (insurance) and serves as a reference example. When starting a new project, replace all content with your own domain knowledge and requirements.

If you are starting a new project, use the external `nebula-agents` framework bootstrap docs from your chosen framework repo. Those assets are not vendored into this product repo.

## Minimal Folder Scaffold

```bash
mkdir -p planning-mds/{domain,examples,features,screens,workflows,architecture,api,security,testing,operations,knowledge-graph}
mkdir -p planning-mds/features/archive
mkdir -p planning-mds/examples/{personas,features,stories,screens,architecture,architecture/adrs}
mkdir -p planning-mds/security/reviews
```

Knowledge graph convention:
- `planning-mds/knowledge-graph/solution-ontology.yaml` defines the typed vocabulary and precedence rules.
- `planning-mds/knowledge-graph/canonical-nodes.yaml` seeds shared solution nodes.
- `planning-mds/knowledge-graph/feature-mappings.yaml` maps features and stories into the canonical layer.

Prompt context convention:
- `planning-mds/context-map.yaml` defines default vs on-demand agent context for this product.
- Default context should stay small: product README, lifecycle stage, `.agentignore`, feature registry, roadmap, target feature folder, KG lookup/hint output, and exact changed files.
- Archive, evidence, screenshots, logs, generated artifacts, full API/schema bundles, and full runtime trees are on-demand only.
- Validate the policy with `python3 scripts/validate-context-map.py`.

Architecture governance convention:
- `planning-mds/architecture/SOLUTION-PATTERNS.md` is the product-local implementation pattern source.
- Use `polyglot-service-governance.md`, `microservices-decision-framework.md`, `event-contract-governance.md`, and `deployment-topology-guidance.md` before approving future .NET/Python stack changes, bounded service extraction, or event-driven service boundaries.
- These documents enable future planning only; they do not create services, migrations, or runtime behavior.

Story convention:
- Keep one story per markdown file inside its feature folder: `planning-mds/features/F{NNNN}-{slug}/F{NNNN}-S{NNNN}-{slug}.md`.

## Rule of Thumb

If it’s **project-specific**, it belongs here. Agents should never embed it directly.

## Token-Saving Retrieval Flow

1. Read `README.md`, `lifecycle-stage.yaml`, `.agentignore`, `planning-mds/context-map.yaml`, `planning-mds/features/REGISTRY.md`, and `planning-mds/features/ROADMAP.md`.
2. Resolve the target feature from the user request, roadmap, registry, or KG lookup.
3. Read only the target feature folder and exact files referenced by its plan, stories, ADRs, KG output, or failing validation.
4. For source code, start from `scripts/kg/hint.py <path>`, `scripts/kg/lookup.py <feature-id>`, changed paths, or exact failing files. Do not broad-load `engine/**`, `experience/**`, `neuron/**`, or future `services/**` trees.
5. For evidence, start from `planning-mds/operations/evidence/README.md`, feature `latest-run.json`, then the selected run `evidence-manifest.json`. Read only exact evidence artifacts named by the manifest or current audit.
6. For archives and examples, read only explicitly named files or feature folders.
