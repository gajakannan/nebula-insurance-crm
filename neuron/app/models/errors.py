"""Normalized model-provider errors (F0039-S0004).

Every provider failure — timeout, connection reset, empty choices, non-object JSON,
auth rejection, budget overrun — is mapped to one of these typed errors. Callers
(S0006's resolver especially) branch on the *type*, never on a provider's raw message,
so swapping vLLM for another OpenAI-compatible server changes no downstream logic.

**These errors never carry raw model content.** `detail` is a fixed, bounded phrase.
A model's raw output is exactly where prompt-injection payloads and customer PII would
appear, so it must not reach an exception string, a log line, or telemetry (ADR-027
security notes). Diagnostics travel as counts, hashes, and status codes instead.
"""

from __future__ import annotations

from ..errors import NeuronError


class ProviderError(NeuronError):
    """Base for every normalized provider failure.

    ``status = 502``: a model failure is an upstream failure from the caller's point of
    view, not the caller's fault.
    """

    status = 502
    title = "Model provider error"
    #: Stable machine-readable code for metrics and the reliability matrix (§27).
    code = "provider_error"


class ProviderTimeoutError(ProviderError):
    """The provider did not respond within the configured timeout."""

    status = 504
    title = "Model provider timed out"
    code = "provider_timeout"


class ProviderUnavailableError(ProviderError):
    """The provider was unreachable (connection refused/reset, DNS, transport error)."""

    title = "Model provider unavailable"
    code = "provider_unavailable"


class ProviderAuthError(ProviderError):
    """The provider rejected our credentials (401/403).

    Distinct from ``ProviderUnavailableError`` because retrying is pointless and the
    operational fix is different — but the message still names no credential.
    """

    title = "Model provider rejected credentials"
    code = "provider_auth"


class ProviderEmptyResponseError(ProviderError):
    """The provider returned a response with no usable choice."""

    title = "Model provider returned no completion"
    code = "provider_empty"


class ProviderInvalidJsonError(ProviderError):
    """The completion was not a JSON **object**.

    Covers unparseable text, and valid JSON that is a scalar or array. The resolution
    contract is object-shaped; anything else fails closed here rather than being
    coerced downstream.
    """

    title = "Model provider returned malformed output"
    code = "provider_invalid_json"


class ProviderBudgetError(ProviderError):
    """The request would exceed the model's context budget.

    Raised **before** the call: a request that cannot fit is a caller bug, and sending
    it anyway would burn latency to earn a truncated answer.
    """

    status = 400
    title = "Model request exceeds the token budget"
    code = "provider_budget"


class ProviderConfigError(ProviderError):
    """The provider profile is missing or internally inconsistent."""

    status = 500
    title = "Model provider misconfigured"
    code = "provider_config"
