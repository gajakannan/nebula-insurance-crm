# F0034 - Product Schema Registry and Dynamic LOB Attributes - Getting Started

## Prerequisites

- [ ] Read [PRD.md](./PRD.md).
- [ ] Read [lob-extensible-attribute-plan.md](../../architecture/lob-extensible-attribute-plan.md).
- [ ] Review the F0020 document metadata schema renderer as an adjacent but narrower precedent.
- [ ] Review F0019 to identify where quote/proposal fields would otherwise become hardcoded product attributes.

## Product Manager Refinement Questions

1. What is the smallest product pilot that proves the registry and dynamic form pattern?
2. Which lifecycle carrier gets the first persisted `attributes` payload?
3. What widget vocabulary is required for the pilot, and which widgets can wait?
4. What validation parity fixtures are required before implementation can be accepted?
5. What compatibility behavior is required for existing records that only have static `lineOfBusiness`?

## How to Verify Planning Readiness

1. Confirm `ROADMAP.md` lists F0034 in `Now`.
2. Confirm `REGISTRY.md` assigns F0034 and advances the next available feature number.
3. Confirm the full planning pass creates story files and refreshes `STORY-INDEX.md`.
4. Confirm F0019 planning references the F0034 foundation instead of adding hardcoded product attributes.
