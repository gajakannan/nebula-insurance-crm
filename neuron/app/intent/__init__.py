"""Intent resolution — catalog, prompts, contracts, and fail-closed validation (F0039).

The trust boundary lives here: the model *proposes* a scope and an intent; this package
decides whether that proposal is admissible. Domains, actions, and specialist head ids
all come from the reviewed `intent-catalog.yaml`, never from model output, and every
decision is validated against a vendored JSON Schema and then against deterministic
cross-field invariants before anything is allowed to route.
"""
