"""Loader for the vendored JSON Schema contracts (``app/contracts/``).

The runtime contracts are vendored from ``planning-mds/schemas/`` so the Neuron
container is self-contained. ``tests/test_schema_drift.py`` guards the vendored copies
against the authoritative planning-mds sources.

Cross-schema ``$ref``s (the composed intent resolution references the scope and intent
sections by ``$id``) resolve from an in-process registry built out of the vendored
files — schema validation never reaches the network, which would make an offline or
air-gapped runtime fail open.
"""

from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path

import jsonschema

CONTRACTS_DIR = Path(__file__).parent / "contracts"

_SCHEMA_FILES = {
    "agent-card": "neuron-agent-card.schema.json",
    "orchestration-plan": "neuron-orchestration-plan.schema.json",
    "message-envelope": "neuron-message-envelope.schema.json",
    "zone-payload": "neuron-zone-payload.schema.json",
    "broker-activity-list": "neuron-broker-activity-list.schema.json",
    "companion-telemetry-event": "neuron-companion-telemetry-event.schema.json",
    # F0039-S0005 — the intent resolution contracts.
    "scope-decision": "neuron-scope-decision.schema.json",
    "intent-decision": "neuron-intent-decision.schema.json",
    "intent-resolution": "neuron-intent-resolution.schema.json",
}


@lru_cache(maxsize=None)
def load_schema(name: str) -> dict:
    try:
        filename = _SCHEMA_FILES[name]
    except KeyError:
        raise KeyError(f"unknown vendored schema {name!r}") from None
    return json.loads((CONTRACTS_DIR / filename).read_text(encoding="utf-8"))


@lru_cache(maxsize=None)
def _registry():
    """Every vendored schema, keyed by its ``$id``, for offline ``$ref`` resolution."""
    from referencing import Registry, Resource

    resources = []
    for filename in _SCHEMA_FILES.values():
        schema = json.loads((CONTRACTS_DIR / filename).read_text(encoding="utf-8"))
        schema_id = schema.get("$id")
        if schema_id:
            resources.append((schema_id, Resource.from_contents(schema)))
    return Registry().with_resources(resources)


def _inline_refs(node, by_id: dict, depth: int = 0):
    """Recursively replace ``{"$ref": "<$id>"}`` with the referenced schema's contents.

    WHY this exists: a server-side structured-output backend (vLLM/xgrammar) compiles a
    grammar from the schema it is given and **cannot fetch an external ``$id``**. Handed
    a ``$ref`` it cannot resolve, it silently emits an unconstrained grammar for that
    subtree — the request looks structured and the output is not. Verified against the
    live endpoint: the composed schema came back with ``"scope": "redirect"`` as a bare
    string instead of the required object.

    Local validation still uses the *referenced* form via the registry; only the copy
    sent over the wire is inlined, so the authored contracts stay separate files.
    """
    if depth > 10:  # pragma: no cover - guards a pathological cyclic schema
        raise ValueError("schema $ref nesting too deep to inline")
    if isinstance(node, list):
        return [_inline_refs(item, by_id, depth + 1) for item in node]
    if not isinstance(node, dict):
        return node
    ref = node.get("$ref")
    if isinstance(ref, str) and ref in by_id:
        target = _inline_refs(by_id[ref], by_id, depth + 1)
        merged = {k: v for k, v in target.items() if k not in ("$schema", "$id")}
        # Sibling keys alongside $ref (e.g. a local description) win over the target's.
        merged.update({k: v for k, v in node.items() if k != "$ref"})
        return merged
    return {k: _inline_refs(v, by_id, depth + 1) for k, v in node.items()}


@lru_cache(maxsize=None)
def load_bundled_schema(name: str) -> dict:
    """A self-contained copy of a schema with every local ``$ref`` inlined.

    This is what gets sent to a model provider for structured output. Never used for
    validation — validation resolves refs properly through the registry.
    """
    by_id = {}
    for filename in _SCHEMA_FILES.values():
        schema = json.loads((CONTRACTS_DIR / filename).read_text(encoding="utf-8"))
        if schema.get("$id"):
            by_id[schema["$id"]] = schema
    return _inline_refs(load_schema(name), by_id)


@lru_cache(maxsize=None)
def get_validator(name: str) -> jsonschema.protocols.Validator:
    schema = load_schema(name)
    validator_cls = jsonschema.validators.validator_for(schema)
    validator_cls.check_schema(schema)
    return validator_cls(schema, registry=_registry())
