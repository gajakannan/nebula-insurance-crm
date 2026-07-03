"""Renewal outreach drafter — crm.renewals.outreach_drafter goal agent (F0038-S0005/S0006).

Generate-on-action: produces a content-safe outreach draft (LLM mocked for this run),
persists it engine-first (ADR-028 §2), and — on send — commits the mock-send. A Neuron-side
content guard mirrors the engine's (defense in depth): no premium/quote/coverage-terms/binding
/currency language ever leaves the drafter.
"""

from __future__ import annotations

import hashlib
import re

DRAFTER_CARD_ID = "crm.renewals.outreach_drafter"
DRAFT_COMPONENT = "outreach.draft_editor"
PROMPT_ID = "renewal-outreach"
PROMPT_VERSION = "1.0.0"

_FORBIDDEN = re.compile(
    r"\b(premiums?|quot(e|es|ed|ing)|bind|binding|bound|deductible|coverage limit|policy limit|terms and conditions)\b|\$\s?\d",
    re.IGNORECASE,
)


def content_violation(body: str | None) -> str | None:
    """Returns a reason when the body breaks the content constraint, else None."""
    if not body or not body.strip():
        return "empty draft body"
    if _FORBIDDEN.search(body):
        return "draft must not include premium/quote/coverage-terms/binding language"
    return None


def generate_draft_body(account_name: str | None) -> str:
    # Deterministic, content-safe outreach template (LLM mocked). Intentionally states no
    # premium/quote/terms/binding — the human edits before mock-send.
    name = account_name or "your client"
    return (
        f"Hi, I'm reaching out about {name}'s upcoming policy renewal. "
        "Could we connect to start the renewal conversation and make sure everything is on track?"
    )


def content_hash(body: str) -> str:
    return "sha256:" + hashlib.sha256(body.encode("utf-8")).hexdigest()
