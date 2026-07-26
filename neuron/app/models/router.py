"""Model router seam (F0038-S0001, extended for structured output in F0039-S0004).

Selects a provider by config. Every result carries provenance-safe metadata (model,
revision, token counts, cost, latency, content hash) — **never raw prompt/response
text**.

F0039 adds the *structured* completion contract: an async call that takes a JSON Schema
and returns a parsed object plus provenance. That is the seam the intent resolver
(S0006) builds on, and it is deliberately narrow — a provider either returns an object
that satisfied the schema request, or it raises a normalized error from
``app.models.errors``. There is no "best effort" middle state for a caller to
misinterpret.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from typing import Any, Protocol, runtime_checkable

from ..errors import ConfigError
from .errors import ProviderBudgetError, ProviderInvalidJsonError

# Phi-4-mini-instruct serves a 4,096-token context (verified at G1). The budget is
# enforced client-side so an over-long request fails fast instead of being truncated
# into a plausible-looking but incomplete answer.
DEFAULT_CONTEXT_LIMIT = 4096
# Rough chars-per-token for pre-flight budgeting. Deliberately conservative: this
# guards against overrun, it is not an accounting figure (real counts come back in
# provenance from the server's own tokenizer).
_CHARS_PER_TOKEN = 3.5


@dataclass(frozen=True)
class ModelResult:
    model: str
    content: str
    content_hash: str
    prompt_tokens: int = 0
    completion_tokens: int = 0
    cost: float = 0.0
    latency_ms: int = 0


@dataclass(frozen=True)
class ModelProvenance:
    """Everything recorded about a model call — and nothing that could carry content.

    ``model_revision`` and ``image_digest`` are *configured*, not discovered: an
    OpenAI-compatible server reports the model id and context length but not a pinned
    HF commit or the container digest it runs in (S0004). Recording them from config is
    what makes a result reproducible later.
    """

    model: str
    content_hash: str
    prompt_tokens: int = 0
    completion_tokens: int = 0
    total_tokens: int = 0
    latency_ms: int = 0
    cost: float = 0.0
    model_revision: str | None = None
    image_digest: str | None = None
    request_id: str | None = None
    finish_reason: str | None = None
    # True when the call was retried once after a pre-response connection reset.
    retried: bool = False


@dataclass(frozen=True)
class StructuredModelResult:
    """A parsed JSON **object** plus its provenance.

    ``data`` is guaranteed to be a ``dict`` — providers raise
    ``ProviderInvalidJsonError`` rather than hand back a scalar, an array, or ``None``.
    Schema *conformance* is a separate concern: the resolver re-validates against the
    vendored schema (S0006) and never trusts the model to have honoured it.
    """

    data: dict[str, Any]
    provenance: ModelProvenance
    raw_fields: dict[str, Any] = field(default_factory=dict)


def content_hash(text: str) -> str:
    return "sha256:" + hashlib.sha256(text.encode("utf-8")).hexdigest()


def estimate_tokens(text: str) -> int:
    """Conservative pre-flight token estimate (over-estimates rather than under)."""
    if not text:
        return 0
    return int(len(text) / _CHARS_PER_TOKEN) + 1


def enforce_budget(
    *,
    system: str | None,
    prompt: str,
    max_tokens: int,
    context_limit: int = DEFAULT_CONTEXT_LIMIT,
) -> int:
    """Fail closed when prompt + reservation cannot fit the context window.

    Returns the estimated prompt tokens so the caller can record it. Raising here —
    before any network call — is deliberate: a request that cannot fit will come back
    truncated, and a truncated structured answer is worse than no answer because it can
    still parse.
    """
    if max_tokens <= 0:
        raise ProviderBudgetError("max_tokens must be positive")
    if max_tokens >= context_limit:
        raise ProviderBudgetError(
            f"max_tokens {max_tokens} leaves no room in a {context_limit}-token context"
        )
    estimated = estimate_tokens(system or "") + estimate_tokens(prompt)
    if estimated + max_tokens > context_limit:
        raise ProviderBudgetError(
            f"request needs about {estimated + max_tokens} tokens, over the "
            f"{context_limit}-token context budget"
        )
    return estimated


def parse_structured_content(content: str) -> dict[str, Any]:
    """Parse a completion into a JSON object, or fail closed.

    Tolerates a fenced ```json block because some servers wrap output, but does not
    attempt to repair malformed JSON: silently "fixing" a model's output is how an
    injected payload becomes a trusted object.
    """
    text = (content or "").strip()
    if text.startswith("```"):
        # Strip a leading fence line and a trailing fence.
        lines = text.splitlines()
        if len(lines) >= 2:
            lines = lines[1:]
            if lines and lines[-1].strip().startswith("```"):
                lines = lines[:-1]
            text = "\n".join(lines).strip()
    if not text:
        raise ProviderInvalidJsonError("completion was empty")
    try:
        parsed = json.loads(text)
    except (ValueError, TypeError) as exc:
        # NOTE: the raw text is deliberately NOT included — it is attacker-influenced.
        raise ProviderInvalidJsonError("completion was not valid JSON") from exc
    if not isinstance(parsed, dict):
        raise ProviderInvalidJsonError(
            f"completion was a JSON {type(parsed).__name__}, not an object"
        )
    return parsed


@runtime_checkable
class ModelProvider(Protocol):
    name: str

    def complete(self, prompt: str, *, system: str | None = None, max_tokens: int = 1024) -> ModelResult: ...

    async def complete_structured(
        self,
        *,
        prompt: str,
        schema: dict[str, Any],
        schema_name: str = "decision",
        system: str | None = None,
        max_tokens: int = 512,
    ) -> StructuredModelResult:
        """Return a JSON object conforming to ``schema``, plus provenance."""
        ...


class ModelRouter:
    def __init__(self, providers: dict[str, ModelProvider], default: str) -> None:
        if default not in providers:
            raise ConfigError(f"default model provider {default!r} is not registered")
        self._providers = providers
        self._default = default

    @property
    def default(self) -> str:
        return self._default

    def names(self) -> list[str]:
        return sorted(self._providers)

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

    async def complete_structured(
        self,
        *,
        prompt: str,
        schema: dict[str, Any],
        schema_name: str = "decision",
        provider: str | None = None,
        system: str | None = None,
        max_tokens: int = 512,
    ) -> StructuredModelResult:
        return await self.provider(provider).complete_structured(
            prompt=prompt,
            schema=schema,
            schema_name=schema_name,
            system=system,
            max_tokens=max_tokens,
        )
