"""Agent + tool registries (F0038-S0001).

Every specialist head, tool, and terminal-state a YAML plan references must resolve
here, or the plan is rejected at startup. The ``/health`` endpoint reports the
registered heads and tools to prove the orchestration assets validated.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, Protocol

from ..errors import ConfigError
from .agent_card import AgentCard
from .heads import AgentHandler


@dataclass(frozen=True)
class RegisteredAgent:
    card: AgentCard
    handler: AgentHandler


class ToolLike(Protocol):
    name: str


class AgentRegistry:
    """card_id → (AgentCard, handler)."""

    def __init__(self) -> None:
        self._agents: dict[str, RegisteredAgent] = {}

    def register(self, card: AgentCard, handler: AgentHandler) -> None:
        if card.card_id in self._agents:
            raise ConfigError(f"agent {card.card_id!r} already registered")
        self._agents[card.card_id] = RegisteredAgent(card=card, handler=handler)

    def has(self, card_id: str) -> bool:
        return card_id in self._agents

    def get(self, card_id: str) -> RegisteredAgent:
        return self._agents[card_id]

    def card_ids(self) -> list[str]:
        return sorted(self._agents)

    def heads(self) -> list[str]:
        """Registered specialist-head card ids (what ``/health`` advertises)."""
        return sorted(
            cid for cid, a in self._agents.items() if a.card.kind == "specialist_head"
        )

    def active_heads(self) -> list[str]:
        return sorted(
            cid
            for cid, a in self._agents.items()
            if a.card.kind == "specialist_head" and a.card.active
        )


class ToolRegistry:
    """tool_name → tool descriptor. Tools are MCP-shaped engine capabilities (F0038-S0001)."""

    def __init__(self) -> None:
        self._tools: dict[str, ToolLike] = {}

    def register(self, tool: ToolLike) -> None:
        if tool.name in self._tools:
            raise ConfigError(f"tool {tool.name!r} already registered")
        self._tools[tool.name] = tool

    def register_all(self, tools: Iterable[ToolLike]) -> None:
        for tool in tools:
            self.register(tool)

    def has(self, name: str) -> bool:
        return name in self._tools

    def get(self, name: str) -> ToolLike:
        return self._tools[name]

    def names(self) -> list[str]:
        return sorted(self._tools)
