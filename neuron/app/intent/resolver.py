"""One-call direct scope-and-intent resolver (F0039-S0006, spec §11.8).

The composed call: **one** physical Phi generation returns a `scope` section and an
`intent` section, which are then validated independently. One call because the local
latency budget is spent once; two sections because the stages have genuinely different
contracts, telemetry, and failure rules.

What the model receives is deliberately minimal — the normalized message and the
registered active catalog. **No CRM records, no user token, no tool handles, no
conversation history.** The model cannot leak what it was never given, and this is why a
successful prompt injection still cannot reach customer data: the worst it can do is
propose a route, and the catalog and invariants decide whether that proposal survives.

Every failure path — timeout, provider down, malformed output, invariant violation —
produces a bounded redirect or clarify and makes **no engine call**.
"""

from __future__ import annotations

import time
from dataclasses import dataclass
from typing import Any

from ..models.errors import ProviderError
from ..models.router import ModelProvenance, ModelRouter
from ..schemas import load_bundled_schema
from .catalog import IntentCatalog
from .contracts import ResolvedIntent, safe_resolution
from .preflight import PreflightDecision, PreflightLimits, run_preflight
from .prompt_registry import INTENT_RESOLVER_PROMPT, Prompt
from .validation import resolve as validate_resolution

# Rejection codes for outcomes that never reached validation.
R_PREFLIGHT_OVERRIDE = "preflight_instruction_override"
R_PROVIDER_FAILURE = "provider_failure"


@dataclass(frozen=True)
class ResolutionOutcome:
    """A validated routing decision plus everything needed to explain it later.

    ``provenance`` is the **single** physical model call. Both logical stages reference
    it, which is what the story means by "the logical scope and intent stages reference
    the same physical model-call provenance".
    """

    resolution: ResolvedIntent
    provenance: ModelProvenance | None = None
    prompt_reference: str | None = None
    prompt_hash: str | None = None
    catalog_version: str | None = None
    catalog_hash: str | None = None
    preflight: PreflightDecision | None = None
    latency_ms: int = 0
    # Why we ended here, in codes only — never user or model text.
    rejection_codes: tuple[str, ...] = ()

    @property
    def should_route(self) -> bool:
        return self.resolution.should_route

    def provenance_fields(self) -> dict[str, Any]:
        """Flat, content-free provenance for the operation store (S0007)."""
        return {
            "prompt_id": self.prompt_reference,
            "prompt_hash": self.prompt_hash,
            "catalog_version": self.catalog_version,
            "catalog_hash": self.catalog_hash,
            "model": self.provenance.model if self.provenance else None,
            "model_revision": self.provenance.model_revision if self.provenance else None,
            "content_hash": self.provenance.content_hash if self.provenance else None,
            "prompt_tokens": self.provenance.prompt_tokens if self.provenance else 0,
            "completion_tokens": self.provenance.completion_tokens if self.provenance else 0,
            "latency_ms": self.latency_ms,
            "scope_decision": self.resolution.scope.decision,
            "scope_reason": self.resolution.scope.reason_code,
            "intent_decision": self.resolution.intent.decision,
            "rejection_codes": list(self.rejection_codes),
        }


class IntentResolver:
    """Preflight → one structured Phi call → deterministic validation."""

    def __init__(
        self,
        *,
        model_router: ModelRouter,
        catalog: IntentCatalog,
        prompt: Prompt,
        provider: str | None = None,
        limits: PreflightLimits | None = None,
        max_output_tokens: int = 512,
    ) -> None:
        self._router = model_router
        self._catalog = catalog
        self._prompt = prompt
        self._provider = provider
        self._limits = limits or PreflightLimits()
        self._max_output_tokens = max_output_tokens

    def build_system_prompt(self) -> str:
        """Render the versioned prompt with the trusted catalog.

        The catalog is injected here, from the reviewed asset — never assembled from
        anything the user supplied.
        """
        return self._prompt.text.replace("{catalog}", self._catalog.describe_for_prompt())

    async def resolve(
        self, text: str | None, *, rate_limited: bool = False
    ) -> ResolutionOutcome:
        started = time.monotonic()

        preflight = run_preflight(text, limits=self._limits, rate_limited=rate_limited)
        if not preflight.should_continue:
            codes = (
                (R_PREFLIGHT_OVERRIDE,)
                if preflight.reason_code == "obvious_instruction_override"
                else (preflight.reason_code,)
            )
            # No model call, no engine call — preflight already decided.
            return ResolutionOutcome(
                resolution=safe_resolution(*codes),
                preflight=preflight,
                catalog_version=self._catalog.catalog_version,
                catalog_hash=self._catalog.content_hash,
                latency_ms=int((time.monotonic() - started) * 1000),
                rejection_codes=codes,
            )

        try:
            result = await self._router.complete_structured(
                prompt=preflight.normalized_text or "",
                # Inlined: a guided-decoding backend cannot resolve an external $ref
                # and would silently emit an unconstrained grammar for that subtree.
                schema=load_bundled_schema("intent-resolution"),
                schema_name="neuron_intent_resolution",
                provider=self._provider,
                system=self.build_system_prompt(),
                max_tokens=self._max_output_tokens,
            )
        except ProviderError as exc:
            # Timeout, unreachable, malformed, over-budget — all bounded, no engine call.
            code = getattr(exc, "code", R_PROVIDER_FAILURE)
            return ResolutionOutcome(
                resolution=safe_resolution(R_PROVIDER_FAILURE, code),
                preflight=preflight,
                prompt_reference=self._prompt.reference,
                prompt_hash=self._prompt.content_hash,
                catalog_version=self._catalog.catalog_version,
                catalog_hash=self._catalog.content_hash,
                latency_ms=int((time.monotonic() - started) * 1000),
                rejection_codes=(R_PROVIDER_FAILURE, code),
            )

        # The endpoint honours `strict: true`, but conformance is re-checked here anyway:
        # trusting the model to have validated itself would make the guarantee depend on
        # the thing being guarded.
        resolution = validate_resolution(result.data, self._catalog)

        return ResolutionOutcome(
            resolution=resolution,
            provenance=result.provenance,
            prompt_reference=self._prompt.reference,
            prompt_hash=self._prompt.content_hash,
            catalog_version=self._catalog.catalog_version,
            catalog_hash=self._catalog.content_hash,
            preflight=preflight,
            latency_ms=int((time.monotonic() - started) * 1000),
            rejection_codes=resolution.rejection_codes,
        )


def build_resolver(runtime, *, provider: str | None = None) -> IntentResolver:
    """Construct the resolver from an assembled runtime (catalog + prompts already loaded)."""
    if runtime.intent_catalog is None:  # pragma: no cover - startup guarantees it
        raise ValueError("runtime has no intent catalog")
    return IntentResolver(
        model_router=runtime.model_router,
        catalog=runtime.intent_catalog,
        prompt=runtime.prompts[INTENT_RESOLVER_PROMPT],
        provider=provider,
    )
