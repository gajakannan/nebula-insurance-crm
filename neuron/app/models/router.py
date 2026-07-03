"""Model router seam (F0038-S0001).

Selects a provider by config. F0038 wires only the deterministic ``mock`` provider;
adding ``claude``/``ollama`` is a registration change, not a caller change. Every
result carries provenance-safe metadata (model, token counts, cost, latency,
content hash) — never raw prompt/response text.
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass
from typing import Protocol, runtime_checkable

from ..errors import ConfigError


@dataclass(frozen=True)
class ModelResult:
    model: str
    content: str
    content_hash: str
    prompt_tokens: int = 0
    completion_tokens: int = 0
    cost: float = 0.0
    latency_ms: int = 0


def content_hash(text: str) -> str:
    return "sha256:" + hashlib.sha256(text.encode("utf-8")).hexdigest()


@runtime_checkable
class ModelProvider(Protocol):
    name: str

    def complete(self, prompt: str, *, system: str | None = None, max_tokens: int = 1024) -> ModelResult: ...


class ModelRouter:
    def __init__(self, providers: dict[str, ModelProvider], default: str) -> None:
        if default not in providers:
            raise ConfigError(f"default model provider {default!r} is not registered")
        self._providers = providers
        self._default = default

    @property
    def default(self) -> str:
        return self._default

    def provider(self, name: str | None = None) -> ModelProvider:
        key = name or self._default
        try:
            return self._providers[key]
        except KeyError:
            raise ConfigError(f"unknown model provider {key!r}") from None

    def complete(
        self,
        prompt: str,
        *,
        provider: str | None = None,
        system: str | None = None,
        max_tokens: int = 1024,
    ) -> ModelResult:
        return self.provider(provider).complete(prompt, system=system, max_tokens=max_tokens)
