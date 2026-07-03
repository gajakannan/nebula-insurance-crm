"""CRM scope guard + intent classifier (F0038-S0007).

Keeps the companion a **CRM-scoped assistant, never a general chatbot** (intake L8).
Every inbound message is classified into a bounded CRM intent taxonomy *before* any
handler runs:

- an in-scope CRM intent routes to the matching specialist head/zone;
- an out-of-scope or prompt-injection message is redirected politely — never answered
  as a general assistant, and user-supplied instructions cannot flip the guard;
- an ambiguous message gets a brief CRM-framed clarifying question;
- a classifier failure fails safe to the redirect path — never an unbounded answer.

The classifier is **deterministic** for this run (the LLM is mocked). It sits behind
the same seam a real LLM classifier would — swapping one in later is a registration
change, not a caller change (mirrors ``models/mock_provider.py``). The guard grants
**no authorization**: in-scope reads still call the engine as the user, which authorizes
them (ADR-027 — the engine is the sole authorization boundary).
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from .orchestration.agent_card import AgentCard

# --- intent taxonomy --------------------------------------------------------

# In-scope CRM intents map 1:1 to a registered specialist-head card.
INTENT_TO_HEAD_CARD = {
    "renewals": "crm.renewals.head",
    "tasks": "crm.tasks.head",
    "pipeline": "crm.pipeline.head",
    "broker_activity": "crm.broker_activity.head",
}

# Non-routing intent labels.
OUT_OF_SCOPE = "out_of_scope"
INJECTION = "injection"
AMBIGUOUS = "ambiguous"

# Guard-decision categories.
ALLOW = "allow"        # in-scope → route to a specialist head
REDIRECT = "redirect"  # out-of-scope / injection → polite CRM redirect
CLARIFY = "clarify"    # ambiguous → CRM-framed clarifying question

_WORD = re.compile(r"[a-z0-9']+")


def _tokens(text: str) -> list[str]:
    return _WORD.findall(text.lower())


# Prompt-injection / scope-escape markers, checked FIRST so a message cannot smuggle
# itself in-scope by appending a CRM word to an instruction to break character. The
# guard is not bypassable by user-supplied instructions (S0007 security AC). Matched
# case-insensitively as substrings of the raw (lowercased) message.
_INJECTION_MARKERS = (
    "ignore previous",
    "ignore all previous",
    "ignore the above",
    "ignore the system",
    "ignore your instruction",
    "disregard previous",
    "disregard your",
    "disregard the above",
    "forget your instruction",
    "forget previous",
    "you are now",
    "you're now",
    "act as a",
    "act as an",
    "act like a",
    "pretend to be",
    "pretend you are",
    "roleplay as",
    "role play as",
    "system prompt",
    "developer mode",
    "jailbreak",
    "dan mode",
    "bypass your",
    "override your",
    "new instructions:",
    "from now on you",
    "as a general assistant",
    "general purpose assistant",
    "general-purpose assistant",
    "reveal your prompt",
    "print your instructions",
)

# In-scope CRM keyword sets. Single-word keywords match on whole-word tokens (so
# "task" never fires on "multitasking"); multi-word keywords match as substrings of
# the normalized text. Renewals is checked first — it is the only live zone.
_RENEWALS_KEYWORDS = (
    "renewal", "renewals", "expiring", "expire", "expires", "expiry", "expiration",
    "outreach", "needs attention", "need attention", "mock send", "send draft",
    "draft outreach", "reach out",
)
_TASKS_KEYWORDS = ("task", "tasks", "todo", "to do", "to-do")
_PIPELINE_KEYWORDS = (
    "pipeline", "submission", "submissions", "quote", "quotes",
    "opportunity", "opportunities", "deal", "deals",
)
_BROKER_KEYWORDS = (
    "broker", "brokers", "broker activity", "broker engagement",
    "broker interaction", "broker follow",
)

# Greeting / capability / meta markers → ambiguous (a CRM-framed clarify, not an answer).
_GREETING_WORDS = {"hi", "hey", "hello", "hiya", "yo", "greetings", "help"}
_GREETING_PHRASES = (
    "what can you do", "what do you do", "who are you", "what are you",
    "how do you work", "help me",
)


def _contains_any(normalized: str, keywords: tuple[str, ...]) -> bool:
    tokens = set(normalized.split())
    for kw in keywords:
        if " " in kw or "-" in kw:
            if kw.replace("-", " ") in normalized:
                return True
        elif kw in tokens:
            return True
    return False


def classify_intent(text: str) -> str:
    """Deterministic CRM intent classification (LLM mocked this run).

    Security-first priority: injection / scope-escape markers are checked before any
    CRM keyword, so a message cannot become in-scope by appending a CRM word to an
    instruction to break character. No CRM signal → ``out_of_scope`` (the safe default;
    never a general answer).
    """
    normalized = " ".join(_tokens(text))
    if not normalized:
        return AMBIGUOUS  # empty / punctuation-only → ask to rephrase (still bounded)

    lowered = text.lower()

    # 1. Injection / scope-escape — highest priority, not bypassable by user text.
    if any(marker in lowered for marker in _INJECTION_MARKERS):
        return INJECTION

    # 2. In-scope CRM intents (renewals first — the live zone).
    if _contains_any(normalized, _RENEWALS_KEYWORDS):
        return "renewals"
    if _contains_any(normalized, _TASKS_KEYWORDS):
        return "tasks"
    if _contains_any(normalized, _PIPELINE_KEYWORDS):
        return "pipeline"
    if _contains_any(normalized, _BROKER_KEYWORDS):
        return "broker_activity"

    # 3. Greeting / capability / meta question → ambiguous (CRM-framed clarify).
    if set(normalized.split()) & _GREETING_WORDS or any(p in normalized for p in _GREETING_PHRASES):
        return AMBIGUOUS

    # 4. Default: no CRM signal → out of scope (safe; never a general answer).
    return OUT_OF_SCOPE


# --- guard decision + copy --------------------------------------------------

REDIRECT_TEXT = (
    "I'm your CRM companion, so I can help with your renewals, outreach, and broker "
    "follow-ups — but not with that. Want me to pull up the renewals that need your "
    "attention?"
)
CLARIFY_TEXT = (
    "I can help with your CRM work — renewals that need attention, outreach drafts, and "
    "broker follow-ups. Which of those would you like to start with?"
)


@dataclass(frozen=True)
class GuardDecision:
    """The scope-guard verdict for one inbound message."""

    category: str  # ALLOW | REDIRECT | CLARIFY
    intent: str  # bounded taxonomy label, recorded for observability
    target_head_card_id: str | None = None  # set iff category == ALLOW
    reply_text: str | None = None  # set for REDIRECT / CLARIFY

    @property
    def in_scope(self) -> bool:
        return self.category == ALLOW


def evaluate_scope(text: str) -> GuardDecision:
    """Classify + apply the scope policy, failing safe to a redirect on any error."""
    try:
        intent = classify_intent(text or "")
    except Exception:
        # Fail safe (S0007 reliability): a classifier failure NEVER falls through to an
        # unbounded general answer — it becomes the bounded redirect.
        return GuardDecision(REDIRECT, OUT_OF_SCOPE, reply_text=REDIRECT_TEXT)

    if intent in INTENT_TO_HEAD_CARD:
        return GuardDecision(ALLOW, intent, target_head_card_id=INTENT_TO_HEAD_CARD[intent])
    if intent == AMBIGUOUS:
        return GuardDecision(CLARIFY, AMBIGUOUS, reply_text=CLARIFY_TEXT)
    # out_of_scope OR injection → the same polite redirect (do not reveal guard
    # internals to the caller); the distinct label is still recorded for observability.
    return GuardDecision(REDIRECT, intent, reply_text=REDIRECT_TEXT)


# --- behavioral handlers (bound to the two S0007 cards) ---------------------


class IntentClassifierHandler:
    """Behavioral handler for ``crm.intent_classifier`` (F0038-S0007).

    Deterministic CRM intent classification behind the model-router-style seam. Pure
    classification — no engine data, no authorization (``auth_mode: none``).
    """

    def __init__(self, card: AgentCard) -> None:
        self.card = card

    def classify(self, text: str) -> str:
        return classify_intent(text or "")

    async def handle(self, request):  # AgentHandler protocol
        return self.classify(getattr(request, "text", request))


class ScopeGuardHandler:
    """Behavioral handler for ``crm.scope_guard`` (F0038-S0007).

    Turns a classified intent into a guard decision — allow+route, redirect, or
    clarify — and fails safe to redirect. Grants no authorization: in-scope reads are
    still authorized by the engine as the user.
    """

    def __init__(self, card: AgentCard) -> None:
        self.card = card

    def evaluate(self, text: str) -> GuardDecision:
        return evaluate_scope(text)

    async def handle(self, request):  # AgentHandler protocol
        return self.evaluate(getattr(request, "text", request))
