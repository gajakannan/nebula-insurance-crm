"""Deterministic mock LLM provider (F0038 run assumption: LLM mocked).

Produces stable, replayable output derived from a hash of the prompt so feature
evidence is deterministic and no live Anthropic call is made. It satisfies the
``ModelProvider`` seam; a real client swaps in behind the router without touching
callers. It intentionally emits **no** free-form prose that could carry premium /
quote / terms content — the drafter goal agent (S0005) applies the content guard.
"""

from __future__ import annotations

import hashlib

from .router import ModelResult, content_hash

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
