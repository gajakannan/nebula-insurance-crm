"""Private/internal A2A Agent Cards (ADR-027 §4, neuron-agent-card.schema.json).

Cards are versioned, code-reviewed **source assets** under ``neuron/crm_agents/`` —
never database authoring rows (ADR-027 §9). ``agent_runs``/``provenance`` persist
only a reference (card_id + version + content_hash); the definition stays here.
Public Agent Card discovery is deferred: every F0038 card must declare ``public:
false`` (enforced below).
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import jsonschema
import yaml

from ..errors import CardValidationError
from ..schemas import get_validator


def _content_hash(data: dict[str, Any]) -> str:
    """Stable sha256 over canonical JSON — the replay reference for agent_runs."""
    canonical = json.dumps(data, sort_keys=True, separators=(",", ":"))
    return "sha256:" + hashlib.sha256(canonical.encode("utf-8")).hexdigest()


@dataclass(frozen=True)
class AgentCard:
    card_id: str
    card_version: str
    kind: str
    name: str
    accepted_output_modes: tuple[str, ...]
    content_hash: str
    description: str | None = None
    active: bool = True
    auth_mode: str = "user_token"
    tools: tuple[str, ...] = ()
    delegates_to: tuple[str, ...] = ()
    capabilities: tuple[dict[str, Any], ...] = ()
    public: bool = False
    raw: dict[str, Any] = field(default_factory=dict, repr=False)

    @classmethod
    def from_dict(cls, data: dict[str, Any], *, source: str | None = None) -> "AgentCard":
        where = f" ({source})" if source else ""
        try:
            get_validator("agent-card").validate(data)
        except jsonschema.ValidationError as exc:
            raise CardValidationError(f"agent card failed schema{where}: {exc.message}") from exc
        # WHY: F0038 exposes no public Agent Card — a public:true card must not load,
        # even though the schema permits the field (ADR-027 §2 requires an amendment).
        if data.get("public", False):
            raise CardValidationError(
                f"agent card {data['card_id']!r} declares public:true, forbidden in F0038{where}"
            )
        return cls(
            card_id=data["card_id"],
            card_version=data["card_version"],
            kind=data["kind"],
            name=data["name"],
            accepted_output_modes=tuple(data["accepted_output_modes"]),
            content_hash=_content_hash(data),
            description=data.get("description"),
            active=data.get("active", True),
            auth_mode=data.get("auth_mode", "user_token"),
            tools=tuple(data.get("tools", [])),
            delegates_to=tuple(data.get("delegates_to", [])),
            capabilities=tuple(data.get("capabilities", [])),
            public=data.get("public", False),
            raw=data,
        )


def load_cards(cards_dir: str | Path) -> dict[str, AgentCard]:
    """Load + validate every ``*.yaml`` card under ``cards_dir``.

    Raises ``CardValidationError`` (a ``ConfigError``) for an unreadable directory,
    an unparseable/invalid card, or a duplicate ``card_id`` — the service fails fast
    rather than start half-configured (F0038-S0001).
    """
    cards_dir = Path(cards_dir)
    if not cards_dir.is_dir():
        raise CardValidationError(f"agent-cards directory not found: {cards_dir}")
    cards: dict[str, AgentCard] = {}
    for path in sorted(cards_dir.glob("*.yaml")):
        try:
            data = yaml.safe_load(path.read_text(encoding="utf-8"))
        except yaml.YAMLError as exc:
            raise CardValidationError(f"unparseable agent card {path.name}: {exc}") from exc
        if not isinstance(data, dict):
            raise CardValidationError(f"agent card {path.name} is not a mapping")
        card = AgentCard.from_dict(data, source=path.name)
        if card.card_id in cards:
            raise CardValidationError(f"duplicate card_id {card.card_id!r} in {path.name}")
        cards[card.card_id] = card
    if not cards:
        raise CardValidationError(f"no agent cards found in {cards_dir}")
    return cards
