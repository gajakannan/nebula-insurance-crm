"""Agent handler protocol + bootstrap placeholders (F0038-S0001).

Every registered head/agent resolves to a *handler*. S0001 stands up the runtime,
registries, and validation; the behavioral handlers (Renewals read, stub zones,
outreach drafter, scope guard) land in later F0038 slices. Until then each card is
registered with a ``BootstrapHandler`` so plan/registry reference resolution is
real, while invoking an unimplemented head raises a typed, honest error rather than
silently returning nothing (F0038-S0001: "no silent fallthrough").
"""

from __future__ import annotations

from typing import Any, Protocol, runtime_checkable

from .agent_card import AgentCard


@runtime_checkable
class AgentHandler(Protocol):
    """A registered executor for an Agent Card."""

    card: AgentCard

    async def handle(self, request: Any) -> Any: ...


class BootstrapHandler:
    """Placeholder handler — registered (resolves) but not yet behavioral.

    Its behavior is delivered by a later F0038 story; invoking it before then is a
    programming error, surfaced as ``NotImplementedError`` (never a silent no-op).
    """

    def __init__(self, card: AgentCard, pending_story: str) -> None:
        self.card = card
        self.pending_story = pending_story

    async def handle(self, request: Any) -> Any:
        raise NotImplementedError(
            f"handler for {self.card.card_id!r} is delivered in {self.pending_story}"
        )
