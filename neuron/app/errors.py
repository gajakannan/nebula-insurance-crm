"""Typed Neuron errors mapped to RFC 7807 ProblemDetails (neuron-api.yaml).

Every error surfaced to a caller is a typed ``NeuronError`` — the runtime never
leaks a raw stack trace as a 500 (F0038-S0001 edge cases). ``ConfigError`` is the
fail-fast startup error: an invalid/missing orchestration asset must stop the
service from starting in a half-configured state.
"""

from __future__ import annotations


class NeuronError(Exception):
    """Base for all typed Neuron errors.

    ``status`` is the HTTP status a boundary handler maps this to; ``title`` is a
    stable, non-sensitive summary. ``detail`` must never carry raw prompts,
    credentials, or customer PII.
    """

    status: int = 500
    title: str = "Neuron error"

    def __init__(self, detail: str | None = None) -> None:
        super().__init__(detail or self.title)
        self.detail = detail or self.title

    def to_problem_details(self, instance: str | None = None) -> dict:
        problem = {
            "type": f"https://nebula.local/problems/{type(self).__name__}",
            "title": self.title,
            "status": self.status,
            "detail": self.detail,
        }
        if instance is not None:
            problem["instance"] = instance
        return problem


# --- Startup / configuration (fail-fast) -----------------------------------


class ConfigError(NeuronError):
    """Invalid or unreadable configuration/orchestration asset at startup.

    The service must fail fast and refuse to start rather than serve a
    half-configured runtime (F0038-S0001).
    """

    status = 500
    title = "Neuron configuration error"


class CardValidationError(ConfigError):
    """An Agent Card asset failed schema or policy validation."""

    title = "Invalid agent card"


class PlanValidationError(ConfigError):
    """A YAML orchestration plan failed schema validation."""

    title = "Invalid orchestration plan"


class UnknownReferenceError(ConfigError):
    """A plan references a head/tool/terminal-state that is not registered."""

    title = "Unresolved orchestration reference"


# --- Runtime / request-time ------------------------------------------------


class UnknownActionError(NeuronError):
    """An action callback referenced an unregistered/allow-listed action."""

    status = 400
    title = "Unknown action"


class UpstreamUnavailableError(NeuronError):
    """The .NET engine was unreachable; surfaced as a typed 502, not a stack leak."""

    status = 502
    title = "Engine upstream unavailable"


class UpstreamAuthError(NeuronError):
    """The engine rejected the forwarded user token (401/403 passthrough)."""

    def __init__(self, status: int, detail: str | None = None) -> None:
        self.status = status
        self.title = "Not authorized by engine" if status == 403 else "Unauthorized"
        super().__init__(detail)


class ScopeViolationError(NeuronError):
    """A message was classified out of CRM scope (F0038-S0007).

    Not an HTTP failure — the caller receives a polite redirect envelope — but the
    guard decision is a typed outcome so it can be recorded in the operation store.
    """

    status = 200
    title = "Out of CRM scope"
