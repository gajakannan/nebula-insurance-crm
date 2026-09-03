"""Bounded user-facing copy for non-routed outcomes (F0039-S0006).

Every redirect and clarify the user can see is written here, by hand. The model never
authors user-visible prose in this feature — it returns a decision, and the decision
selects one of these fixed strings.

That has a security consequence worth stating plainly: a redirect caused by an injection
attempt and a redirect caused by an off-topic question return the **same text**.
Differentiating them would tell an attacker which of their inputs was classified as an
attack, which is the single most useful signal for iterating on an evasion.
"""

from __future__ import annotations

from .contracts import ResolvedIntent

# One redirect string for every non-CRM outcome, injection or not.
REDIRECT_TEXT = (
    "I can help with your CRM work — renewals, tasks, pipeline, and broker activity. "
    "Ask me about one of those and I'll take a look."
)

_CLARIFY_TEXT = {
    "ask_crm_area": "Which part of your CRM would you like to look at — renewals, tasks, or pipeline?",
    "ask_user_goal": "What would you like to do with that?",
    "missing_domain": "Which part of your CRM should I look at?",
    "missing_action": "What would you like me to do there?",
    "missing_entity": "Which record should I look at? A renewal, policy number, or account name works.",
    "multiple_domains": "That covers a few areas — which one should I start with?",
    "multiple_candidate_records": "I found more than one possible match — which record did you mean?",
    "unclear_reference": "Could you say which record or area you mean?",
    "unsupported_action": "I can't do that one yet. I can help with renewals, tasks, or pipeline.",
    "unsupported_broker_filter": (
        "Broker activity filters aren't supported yet. I can show the newest 20 "
        "authorized Broker events without a broker, event-type, or date filter."
    ),
    "unsupported_broker_action": (
        "Broker activity is read-only here. I can show the newest authorized Broker "
        "activity, but I can't create, edit, assign, approve, contact, or follow up."
    ),
}

DEFAULT_CLARIFY_TEXT = "Could you tell me a bit more about what you need?"

# Shown when the resolver itself is unavailable (timeout, provider down). Deliberately
# indistinguishable from any other bounded failure.
UNAVAILABLE_TEXT = (
    "That part of your CRM is temporarily unavailable — please try again in a moment."
)


def clarify_text(clarification_code: str | None) -> str:
    return _CLARIFY_TEXT.get(clarification_code or "", DEFAULT_CLARIFY_TEXT)


def reply_text_for(resolution: ResolvedIntent) -> str:
    """The bounded reply for a resolution that will not route."""
    if resolution.should_route:  # pragma: no cover - caller checks first
        raise ValueError("a routed resolution has no canned reply")
    if resolution.scope.decision == "clarify" or resolution.intent.decision == "clarify":
        return clarify_text(
            resolution.intent.clarification_code or resolution.scope.clarification_code
        )
    return REDIRECT_TEXT
