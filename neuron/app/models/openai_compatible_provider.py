"""OpenAI-compatible structured provider — the local Phi profile (F0039-S0004).

Targets any server speaking the OpenAI chat-completions API; the validated profile is
`microsoft/Phi-4-mini-instruct` on vLLM 0.25.1 (see
`neuron/neuron-local-phi-vllm-wsl2-runbook.md`). Structured output is requested via
`response_format: {type: json_schema, strict: true}`, which that server honours — but
conformance is treated as convenient, never trusted: the resolver re-validates every
response against the vendored schema (S0006).

Two deliberate constraints:

* **The API key is never logged, never echoed, and never placed on a command line.** It
  is read from the environment (`~/.neuron-secrets` in local development) and only ever
  written into an `Authorization` header.
* **Raw prompts and completions never reach telemetry.** Provenance carries hashes and
  counts. The one place raw text exists is the in-flight request/response body.

Testable without `httpx`: the transport is an injectable `sender` coroutine, matching
the `EngineClient` pattern, so the contract suite runs with no network and no GPU.
"""

from __future__ import annotations

import asyncio
import time
from dataclasses import dataclass
from typing import Any, Awaitable, Callable

from .errors import (
    ProviderAuthError,
    ProviderConfigError,
    ProviderEmptyResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
)
from .router import (
    DEFAULT_CONTEXT_LIMIT,
    ModelProvenance,
    ModelResult,
    StructuredModelResult,
    content_hash,
    enforce_budget,
    parse_structured_content,
)

# Transport failures that mean "provider unreachable". httpx errors are added when
# httpx is installed; the stdlib set keeps this module importable (and the
# injected-sender tests runnable) without it.
_TRANSPORT_ERRORS: tuple[type[BaseException], ...] = (ConnectionError, OSError)
_TIMEOUT_ERRORS: tuple[type[BaseException], ...] = (asyncio.TimeoutError, TimeoutError)
try:  # pragma: no cover - depends on optional runtime dep
    import httpx

    _TRANSPORT_ERRORS = (*_TRANSPORT_ERRORS, httpx.TransportError)
    _TIMEOUT_ERRORS = (*_TIMEOUT_ERRORS, httpx.TimeoutException)
except ImportError:  # pragma: no cover
    httpx = None  # type: ignore[assignment]


@dataclass(frozen=True)
class ProviderResponse:
    status_code: int
    body: Any


Sender = Callable[..., Awaitable[ProviderResponse]]


@dataclass(frozen=True)
class PhiProfile:
    """A pinned provider profile.

    ``model_revision`` and ``image_digest`` are configuration, not discovery: the server
    reports the model id and context length but not the HF commit it loaded or the
    container digest it runs in. Recording them here is what makes a decision
    reproducible months later.
    """

    base_url: str
    model: str
    api_key: str = ""
    model_revision: str | None = None
    image_digest: str | None = None
    context_limit: int = DEFAULT_CONTEXT_LIMIT
    timeout_s: float = 30.0
    max_output_tokens: int = 512
    name: str = "local_phi"

    def validate(self) -> None:
        if not self.base_url:
            raise ProviderConfigError("provider base_url is required")
        if not self.model:
            raise ProviderConfigError("provider model is required")
        if self.context_limit <= 0:
            raise ProviderConfigError("provider context_limit must be positive")


class OpenAICompatibleProvider:
    """Structured completions over the OpenAI chat-completions API."""

    def __init__(self, profile: PhiProfile, *, sender: Sender | None = None) -> None:
        profile.validate()
        self._profile = profile
        self._sender = sender
        self.name = profile.name

    # --- sync completion (F0038 seam, kept for interface parity) -------------

    def complete(
        self, prompt: str, *, system: str | None = None, max_tokens: int = 1024
    ) -> ModelResult:
        """Not supported: this provider is async-only.

        Raising beats a blocking shim — a sync HTTP call here would stall the event
        loop, which is exactly what F0039 removed from the persistence layer.
        """
        raise ProviderConfigError(
            f"{self.name} is async-only; use complete_structured()"
        )

    # --- structured completion ----------------------------------------------

    async def complete_structured(
        self,
        *,
        prompt: str,
        schema: dict[str, Any],
        schema_name: str = "decision",
        system: str | None = None,
        max_tokens: int = 512,
    ) -> StructuredModelResult:
        profile = self._profile
        capped = min(max_tokens, profile.max_output_tokens)
        # Fail before the network call if the request cannot fit the context window.
        estimated_prompt = enforce_budget(
            system=system,
            prompt=prompt,
            max_tokens=capped,
            context_limit=profile.context_limit,
        )

        messages: list[dict[str, str]] = []
        if system:
            messages.append({"role": "system", "content": system})
        messages.append({"role": "user", "content": prompt})

        payload = {
            "model": profile.model,
            "messages": messages,
            "max_tokens": capped,
            # Deterministic by construction: an intent decision must not vary run to run.
            "temperature": 0,
            "response_format": {
                "type": "json_schema",
                "json_schema": {"name": schema_name, "strict": True, "schema": schema},
            },
        }

        started = time.monotonic()
        response, retried = await self._send_with_one_retry(payload)
        latency_ms = int((time.monotonic() - started) * 1000)

        if response.status_code in (401, 403):
            raise ProviderAuthError("provider rejected the configured credentials")
        if response.status_code >= 400:
            raise ProviderUnavailableError(
                f"provider returned HTTP {response.status_code}"
            )

        body = response.body if isinstance(response.body, dict) else {}
        choices = body.get("choices") or []
        if not choices:
            raise ProviderEmptyResponseError("provider returned no choices")
        message = (choices[0] or {}).get("message") or {}
        content = message.get("content")
        if not content:
            raise ProviderEmptyResponseError("provider returned an empty completion")

        # Fails closed on anything that is not a JSON object.
        data = parse_structured_content(content)

        usage = body.get("usage") or {}
        provenance = ModelProvenance(
            model=body.get("model") or profile.model,
            content_hash=content_hash(content),
            prompt_tokens=int(usage.get("prompt_tokens") or estimated_prompt),
            completion_tokens=int(usage.get("completion_tokens") or 0),
            total_tokens=int(usage.get("total_tokens") or 0),
            latency_ms=latency_ms,
            cost=0.0,  # Local inference: no per-token vendor cost.
            model_revision=profile.model_revision,
            image_digest=profile.image_digest,
            request_id=body.get("id"),
            finish_reason=(choices[0] or {}).get("finish_reason"),
            retried=retried,
        )
        return StructuredModelResult(data=data, provenance=provenance)

    # --- transport -----------------------------------------------------------

    async def _send_with_one_retry(self, payload: dict[str, Any]) -> tuple[ProviderResponse, bool]:
        """Send, retrying **once** only for a connection reset before any response.

        A timeout is not retried: the server may already be generating, and a second
        request would double the GPU load while the first still runs. Anything past the
        first byte is not retried either — the call is no longer safely repeatable.
        """
        # Timeouts are checked first: TimeoutError subclasses OSError, so the transport
        # clause would otherwise swallow them and wrongly retry.
        try:
            return await self._send(payload), False
        except _TIMEOUT_ERRORS as exc:
            raise ProviderTimeoutError("provider did not respond in time") from exc
        except _TRANSPORT_ERRORS:
            pass  # Connection died before any response — safe to send exactly once more.

        try:
            return await self._send(payload), True
        except _TIMEOUT_ERRORS as exc:
            raise ProviderTimeoutError("provider did not respond in time") from exc
        except _TRANSPORT_ERRORS as exc:
            raise ProviderUnavailableError("provider is unreachable") from exc

    async def _send(self, payload: dict[str, Any]) -> ProviderResponse:
        if self._sender is not None:
            return await self._sender(payload)
        if httpx is None:  # pragma: no cover - only without the optional dep
            raise ProviderConfigError("httpx is required for the live model provider")
        headers = {"Content-Type": "application/json"}
        if self._profile.api_key:
            # The only place the key appears. Never logged, never echoed.
            headers["Authorization"] = f"Bearer {self._profile.api_key}"
        url = self._profile.base_url.rstrip("/") + "/chat/completions"
        async with httpx.AsyncClient(timeout=self._profile.timeout_s) as client:
            raw = await client.post(url, json=payload, headers=headers)
            try:
                body = raw.json()
            except ValueError:
                body = {}
            return ProviderResponse(status_code=raw.status_code, body=body)

    # --- health --------------------------------------------------------------

    async def health(self) -> dict[str, Any]:
        """Readiness detail for `/ready` — reachability plus the pinned profile."""
        detail: dict[str, Any] = {
            "provider": self.name,
            "model": self._profile.model,
            "model_revision": self._profile.model_revision,
            "image_digest": self._profile.image_digest,
            "context_limit": self._profile.context_limit,
        }
        try:
            await self._send({"model": self._profile.model, "messages": [], "max_tokens": 1})
            detail["reachable"] = True
        except Exception:
            # Health must never raise — an unreachable model degrades readiness, it
            # does not crash the probe.
            detail["reachable"] = False
        return detail
