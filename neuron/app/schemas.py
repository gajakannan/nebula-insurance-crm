"""Loader for the vendored JSON Schema contracts (``app/contracts/``).

The four runtime contracts (agent card, orchestration plan, message envelope, zone
payload) are vendored from ``planning-mds/schemas/`` so the Neuron container is
self-contained. ``tests/test_schema_drift.py`` guards the vendored copies against
the authoritative planning-mds sources.
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
}


@lru_cache(maxsize=None)
def load_schema(name: str) -> dict:
    try:
        filename = _SCHEMA_FILES[name]
    except KeyError:
        raise KeyError(f"unknown vendored schema {name!r}") from None
    return json.loads((CONTRACTS_DIR / filename).read_text(encoding="utf-8"))


@lru_cache(maxsize=None)
def get_validator(name: str) -> jsonschema.protocols.Validator:
    schema = load_schema(name)
    validator_cls = jsonschema.validators.validator_for(schema)
    validator_cls.check_schema(schema)
    return validator_cls(schema)
