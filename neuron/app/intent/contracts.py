"""Typed resolution contracts + safe fallbacks (F0039-S0005, spec §10.3/§11.4/§11.8).

These dataclasses are what the rest of Neuron consumes. Model output becomes one of
these only after schema validation **and** the deterministic invariants have passed; a
caller holding a `ScopeDecision` can rely on it being internally consistent, because an
inconsistent one never gets built — it is replaced by a safe redirect.

`allow` here means only "eligible to continue through bounded CRM routing" (spec §7.4).
It grants no authorization: the engine still authorizes every read and write as the user.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

SCHEMA_VERSION = "1.0.0"

# Scope
SCOPE_DECISIONS = ("allow", "clarify", "redirect")
SCOPES = ("crm", "non_crm", "suspicious", "ambiguous")
SCOPE_REASON_CODES = (
    "in_scope",
    "out_of_scope",
    "instruction_override",
    "prompt_disclosure",
    "tool_manipulation",
    "data_exfiltration",
    "identity_override",
    "ambiguous",
)
CLARIFICATION_CODES_SCOPE = (None, "ask_crm_area", "ask_user_goal")

# Intent
INTENT_DECISIONS = ("route", "clarify", "redirect", "adjudicate")
CLARIFICATION_CODES_INTENT = (
    None,
    "missing_domain",
    "missing_action",
    "missing_entity",
    "multiple_domains",
    "multiple_candidate_records",
    "unclear_reference",
    "unsupported_action",
    "unsupported_broker_filter",
    "unsupported_broker_action",
)

# Internal-only reason code. Deliberately NOT in the model-facing schema (spec §10.4):
# the model must never be able to *claim* its own output was invalid.
REASON_INVALID_MODEL_OUTPUT = "invalid_model_output"


@dataclass(frozen=True)
class ScopeDecision:
    decision: str
    scope: str
    reason_code: str
    requires_intent_resolution: bool
    clarification_code: str | None = None
    schema_version: str = SCHEMA_VERSION

    @property
    def allowed(self) -> bool:
        return self.decision == "allow"


@dataclass(frozen=True)
class IntentDecision:
    decision: str
    domain: str | None
    actions: tuple[str, ...] = ()
    secondary_domains: tuple[str, ...] = ()
    entities: dict[str, Any] = field(default_factory=dict)
    needs_context: bool = False
    needs_adjudication: bool = False
    clarification_code: str | None = None
    schema_version: str = SCHEMA_VERSION

    @property
    def routed(self) -> bool:
        return self.decision == "route"


@dataclass(frozen=True)
class ResolvedIntent:
    """The composed, fully validated result the dispatcher acts on.

    ``target_head_card_id`` is resolved from the trusted catalog, never from model
    output. ``requires_confirmation`` is set when any routed action is write-like.
    """

    scope: ScopeDecision
    intent: IntentDecision
    target_head_card_id: str | None = None
    requires_confirmation: bool = False
    # Why this resolution ended where it did — for telemetry and the reliability
    # matrix (§27). Carries codes, never raw user or model text.
    rejection_codes: tuple[str, ...] = ()

    @property
    def should_route(self) -> bool:
        return (
            self.scope.allowed
            and self.intent.routed
            and self.target_head_card_id is not None
        )


def safe_scope_redirect(reason_code: str = REASON_INVALID_MODEL_OUTPUT) -> ScopeDecision:
    """The fail-closed scope decision every invariant violation maps to (§10.4)."""
    return ScopeDecision(
        decision="redirect",
        scope="non_crm",
        reason_code=reason_code,
        requires_intent_resolution=False,
        clarification_code=None,
    )


def safe_intent_redirect() -> IntentDecision:
    """A redirect carries no routed domain or actions — that is the invariant."""
    return IntentDecision(
        decision="redirect",
        domain=None,
        actions=(),
        secondary_domains=(),
        entities={},
        needs_context=False,
        needs_adjudication=False,
        clarification_code=None,
    )


def safe_resolution(*rejection_codes: str) -> ResolvedIntent:
    """A fully fail-closed resolution: redirect, no head, nothing executable."""
    return ResolvedIntent(
        scope=safe_scope_redirect(),
        intent=safe_intent_redirect(),
        target_head_card_id=None,
        requires_confirmation=False,
        rejection_codes=tuple(rejection_codes) or (REASON_INVALID_MODEL_OUTPUT,),
    )


def clarify_resolution(clarification_code: str, *rejection_codes: str) -> ResolvedIntent:
    """A bounded clarify — in CRM scope, but not enough to route on."""
    return ResolvedIntent(
        scope=ScopeDecision(
            decision="clarify",
            scope="ambiguous",
            reason_code="ambiguous",
            requires_intent_resolution=False,
            clarification_code="ask_user_goal",
        ),
        intent=IntentDecision(
            decision="clarify",
            domain=None,
            actions=(),
            entities={},
            clarification_code=clarification_code,
        ),
        target_head_card_id=None,
        requires_confirmation=False,
        rejection_codes=tuple(rejection_codes),
    )
