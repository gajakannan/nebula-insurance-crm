"""Fail-closed validation of model resolution output (F0039-S0005, spec §10.4/§11.5).

Two layers, in this order, and the order matters:

1. **JSON Schema** — shape only. It rejects unknown properties and wrong types, but it
   *cannot* enumerate the domains and actions loaded from `intent-catalog.yaml`, so a
   schema-valid output can still name an action that does not exist.
2. **Deterministic invariants** — cross-field consistency (§10.4) and registry/route
   validation against the trusted catalog (§11.5).

Anything that fails either layer becomes a bounded `redirect` or `clarify`. Nothing is
repaired: "fixing" a contradictory decision is how an injected instruction becomes a
routed action. Every rejection is recorded as a code, never as raw text.
"""

from __future__ import annotations

from typing import Any

from ..schemas import get_validator
from .catalog import MAX_ACTIONS_PER_MESSAGE, IntentCatalog
from .contracts import (
    CLARIFICATION_CODES_INTENT,
    CLARIFICATION_CODES_SCOPE,
    INTENT_DECISIONS,
    REASON_INVALID_MODEL_OUTPUT,
    SCHEMA_VERSION,
    SCOPE_DECISIONS,
    SCOPE_REASON_CODES,
    SCOPES,
    IntentDecision,
    ResolvedIntent,
    ScopeDecision,
    clarify_resolution,
    safe_resolution,
)

# Rejection codes — stable identifiers for telemetry and the §27 reliability matrix.
R_SCHEMA_INVALID = "schema_invalid"
R_SCOPE_INVARIANT = "scope_invariant_violation"
R_INTENT_INVARIANT = "intent_invariant_violation"
R_UNKNOWN_DOMAIN = "unknown_domain"
R_INACTIVE_DOMAIN = "inactive_domain"
R_UNKNOWN_ACTION = "unknown_action"
R_INACTIVE_ACTION = "inactive_action"
R_CROSS_DOMAIN_ACTION = "cross_domain_action"
R_TOO_MANY_ACTIONS = "too_many_actions"
R_NO_ACTIONS = "no_actions"
R_MISSING_ENTITY = "missing_required_entity"
R_UNRESOLVED_HEAD = "unresolved_head"
R_CROSS_SECTION = "cross_section_inconsistent"


def validate_schema(payload: Any, key: str = "intent-resolution") -> bool:
    """True when the payload satisfies the vendored JSON Schema."""
    validator = get_validator(key)
    if validator is None:  # pragma: no cover - a missing contract fails at startup
        return False
    try:
        validator.validate(payload)
    except Exception:
        # The raw payload is attacker-influenced; the reason never leaves as text.
        return False
    return True


# --------------------------------------------------------------------------- #
# §10.4 — scope invariants
# --------------------------------------------------------------------------- #


def scope_invariants_hold(section: dict[str, Any]) -> bool:
    """Cross-field consistency for the scope section.

    A model can return a shape-valid decision that contradicts itself — `allow` while
    reporting the message is out of scope, say. Those combinations are exactly what an
    injection attempt produces when it half-succeeds, so they fail closed.
    """
    decision = section.get("decision")
    scope = section.get("scope")
    requires = section.get("requires_intent_resolution")
    clarification = section.get("clarification_code")

    if decision not in SCOPE_DECISIONS or scope not in SCOPES:
        return False
    if section.get("reason_code") not in SCOPE_REASON_CODES:
        return False
    if clarification not in CLARIFICATION_CODES_SCOPE:
        return False
    if not isinstance(requires, bool):
        return False

    if decision == "allow":
        return scope == "crm" and requires is True and clarification is None
    if decision == "redirect":
        return scope in ("non_crm", "suspicious") and requires is False
    if decision == "clarify":
        return scope == "ambiguous" and requires is False and clarification is not None
    return False


def parse_scope(section: dict[str, Any]) -> ScopeDecision | None:
    """A `ScopeDecision` only when every invariant holds; otherwise ``None``."""
    if not scope_invariants_hold(section):
        return None
    return ScopeDecision(
        decision=section["decision"],
        scope=section["scope"],
        reason_code=section["reason_code"],
        requires_intent_resolution=section["requires_intent_resolution"],
        clarification_code=section.get("clarification_code"),
        schema_version=section.get("schema_version", SCHEMA_VERSION),
    )


# --------------------------------------------------------------------------- #
# §11.5 — intent route validation against the trusted catalog
# --------------------------------------------------------------------------- #


def parse_intent(section: dict[str, Any]) -> IntentDecision | None:
    decision = section.get("decision")
    if decision not in INTENT_DECISIONS:
        return None
    if section.get("clarification_code") not in CLARIFICATION_CODES_INTENT:
        return None
    actions = section.get("actions")
    if not isinstance(actions, list) or any(not isinstance(a, str) for a in actions):
        return None
    entities = section.get("entities")
    if entities is not None and not isinstance(entities, dict):
        return None
    domain = section.get("domain")
    if domain is not None and not isinstance(domain, str):
        return None
    return IntentDecision(
        decision=decision,
        domain=domain,
        actions=tuple(actions),
        secondary_domains=tuple(section.get("secondary_domains") or ()),
        entities={k: v for k, v in (entities or {}).items() if v is not None},
        needs_context=bool(section.get("needs_context", False)),
        needs_adjudication=bool(section.get("needs_adjudication", False)),
        clarification_code=section.get("clarification_code"),
        schema_version=section.get("schema_version", SCHEMA_VERSION),
    )


def intent_invariants_hold(intent: IntentDecision) -> bool:
    """Cross-field consistency for the intent section.

    The headline case is the observed regression: a `redirect` that still carries a
    routed domain and actions. A redirect means "nothing executes", so carrying a route
    is contradictory and must not be quietly honoured in either direction.
    """
    if intent.decision == "route":
        # A route with no domain or no action has nothing to execute.
        return bool(intent.domain) and bool(intent.actions)
    if intent.decision == "redirect":
        return not intent.domain and not intent.actions
    if intent.decision == "clarify":
        # A clarify names no executable action and must say what is unclear.
        return not intent.actions and intent.clarification_code is not None
    if intent.decision == "adjudicate":
        return intent.needs_adjudication is True
    return False


def validate_route(
    intent: IntentDecision, catalog: IntentCatalog
) -> tuple[str | None, bool, list[str]]:
    """Validate a routed intent against the catalog (§11.5 rules 1–10).

    Returns ``(target_head_card_id, requires_confirmation, rejection_codes)``. The head
    is resolved from the catalog — a model-produced head id is never consulted.
    """
    rejections: list[str] = []

    domain = catalog.domain(intent.domain)
    if domain is None:
        return None, False, [R_UNKNOWN_DOMAIN]
    if not domain.active:
        return None, False, [R_INACTIVE_DOMAIN]

    if not intent.actions:
        return None, False, [R_NO_ACTIONS]
    if len(intent.actions) > MAX_ACTIONS_PER_MESSAGE:
        return None, False, [R_TOO_MANY_ACTIONS]

    requires_confirmation = False
    for action_id in intent.actions:
        action = catalog.action(action_id)
        if action is None:
            # Covers the invented-action regression fixture.
            rejections.append(R_UNKNOWN_ACTION)
            continue
        if action.domain != domain.domain_id:
            # Cross-domain actions are never silently collapsed into one route.
            rejections.append(R_CROSS_DOMAIN_ACTION)
            continue
        if not action.active:
            rejections.append(R_INACTIVE_ACTION)
            continue
        if action.missing_entities(intent.entities):
            # Missing a required entity is a clarify, never a guess at the record.
            rejections.append(R_MISSING_ENTITY)
            continue
        if action.requires_explicit_confirmation:
            requires_confirmation = True

    if rejections:
        return None, False, rejections

    head = catalog.head_for(domain.domain_id)
    if not head:  # pragma: no cover - load_catalog already guarantees a head
        return None, False, [R_UNRESOLVED_HEAD]
    return head, requires_confirmation, []


# --------------------------------------------------------------------------- #
# Composed resolution (§11.8)
# --------------------------------------------------------------------------- #


def resolve(payload: Any, catalog: IntentCatalog) -> ResolvedIntent:
    """Validate a composed resolver payload end to end, failing closed at every step."""
    if not isinstance(payload, dict) or not validate_schema(payload):
        return safe_resolution(R_SCHEMA_INVALID)

    scope = parse_scope(payload.get("scope") or {})
    if scope is None:
        return safe_resolution(R_SCOPE_INVARIANT)

    intent = parse_intent(payload.get("intent") or {})
    if intent is None or not intent_invariants_hold(intent):
        return safe_resolution(R_INTENT_INVARIANT)

    # Cross-section consistency: the two halves must agree about whether anything is
    # eligible to run. A scope redirect with a routed intent is the contradiction that
    # a partially-successful injection produces.
    if not scope.allowed and intent.routed:
        return safe_resolution(R_CROSS_SECTION)
    if scope.allowed and not scope.requires_intent_resolution:
        return safe_resolution(R_CROSS_SECTION)

    if not scope.allowed:
        # Faithfully carry a non-routing decision through — no head, nothing executable.
        return ResolvedIntent(
            scope=scope, intent=intent, target_head_card_id=None, requires_confirmation=False
        )

    if intent.decision == "clarify":
        return ResolvedIntent(scope=scope, intent=intent, target_head_card_id=None)
    if intent.decision == "adjudicate":
        # S0009 is gated; until it opens, adjudication degrades to a bounded clarify
        # rather than executing anything.
        return clarify_resolution("unclear_reference", "adjudication_gated")
    if intent.decision == "redirect":
        return ResolvedIntent(scope=scope, intent=intent, target_head_card_id=None)

    head, requires_confirmation, rejections = validate_route(intent, catalog)
    if rejections:
        if R_MISSING_ENTITY in rejections:
            # A missing entity is recoverable by asking, so clarify rather than redirect.
            return clarify_resolution("missing_entity", *rejections)
        return safe_resolution(*rejections)

    return ResolvedIntent(
        scope=scope,
        intent=intent,
        target_head_card_id=head,
        requires_confirmation=requires_confirmation,
    )


__all__ = [
    "REASON_INVALID_MODEL_OUTPUT",
    "resolve",
    "validate_schema",
    "validate_route",
    "scope_invariants_hold",
    "intent_invariants_hold",
    "parse_scope",
    "parse_intent",
]
