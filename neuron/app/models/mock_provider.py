"""Deterministic mock LLM provider (F0038 run assumption: LLM mocked).

Produces stable, replayable output derived from a hash of the prompt so feature
evidence is deterministic and no live Anthropic call is made. It satisfies the
``ModelProvider`` seam; a real client swaps in behind the router without touching
callers. It intentionally emits **no** free-form prose that could carry premium /
quote / terms content — the drafter goal agent (S0005) applies the content guard.
"""

from __future__ import annotations

import hashlib
import json
from typing import Any

from .router import (
    ModelProvenance,
    ModelResult,
    StructuredModelResult,
    content_hash,
    enforce_budget,
)

_MODEL_NAME = "mock-deterministic-1"


class MockProvider:
    name = "mock"

    def complete(self, prompt: str, *, system: str | None = None, max_tokens: int = 1024) -> ModelResult:
        seed = hashlib.sha256((system or "").encode("utf-8") + b"\x00" + prompt.encode("utf-8"))
        digest = seed.hexdigest()
        # Deterministic, bounded, content-free stand-in for a model completion.
        text = f"[mock-completion {digest[:12]}]"
        return ModelResult(
            model=_MODEL_NAME,
            content=text,
            content_hash=content_hash(text),
            prompt_tokens=len(prompt.split()),
            completion_tokens=4,
            cost=0.0,
            latency_ms=0,
        )

    async def complete_structured(
        self,
        *,
        prompt: str,
        schema: dict[str, Any],
        schema_name: str = "decision",
        system: str | None = None,
        max_tokens: int = 512,
    ) -> StructuredModelResult:
        """Deterministic structured completion (F0039-S0004, G0 finding D3).

        ``ModelProvider`` is a ``runtime_checkable`` Protocol, so the mock has to
        implement the structured method or it stops satisfying the seam it exists to
        stand in for. The object is built by filling the *schema's own* required
        properties with type-appropriate deterministic values — enums take their first
        member — so it satisfies whatever schema a caller passes without the mock
        needing to know any specific contract.
        """
        enforce_budget(system=system, prompt=prompt, max_tokens=max_tokens)
        seed = hashlib.sha256(
            (system or "").encode("utf-8") + b"\x00" + prompt.encode("utf-8")
        ).hexdigest()
        data = _fill_from_schema(schema, seed)
        text = json.dumps(data, sort_keys=True)
        return StructuredModelResult(
            data=data,
            provenance=ModelProvenance(
                model=_MODEL_NAME,
                content_hash=content_hash(text),
                prompt_tokens=len(prompt.split()),
                completion_tokens=len(text.split()),
                total_tokens=len(prompt.split()) + len(text.split()),
                latency_ms=0,
                cost=0.0,
                model_revision="mock",
            ),
        )


def _fill_from_schema(schema: dict[str, Any], seed: str) -> dict[str, Any]:
    """Build a deterministic object satisfying a schema's required properties."""
    properties = (schema or {}).get("properties") or {}
    required = (schema or {}).get("required") or list(properties)
    result: dict[str, Any] = {}
    for key in required:
        result[key] = _value_for(properties.get(key) or {}, seed, key)
    return result


def _value_for(spec: dict[str, Any], seed: str, key: str) -> Any:
    enum = spec.get("enum")
    if enum:
        # First member, always — a mock must not appear to "choose".
        return enum[0]
    kind = spec.get("type")
    if isinstance(kind, list):
        kind = next((k for k in kind if k != "null"), "string")
    if kind == "boolean":
        return False
    if kind == "integer":
        return 0
    if kind == "number":
        return 0.0
    if kind == "array":
        return []
    if kind == "object":
        return _fill_from_schema(spec, seed)
    return f"mock-{key}-{seed[:8]}"
