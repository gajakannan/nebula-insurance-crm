"""Intent catalog — the trusted registry of domains and actions (F0039-S0005, spec §11.3).

The catalog is the authority on *what exists*. A model proposes a domain and actions;
this module decides whether they are real, active, and executable. Nothing the model
returns is admitted into routing without matching an entry here, and the specialist head
is resolved from the catalog entry — never from a model-produced head id (§11.5 rule 10).

Loading is **fail-fast**: an invalid catalog raises `ConfigError` at startup so the
service refuses to serve rather than run with a registry it cannot trust. That matches
how Agent Cards, plans, and tools already behave (F0038-S0001).

The registered entity types are fixed here rather than in the catalog because they are
the *schema's* property names — a catalog that referenced an unregistered entity would
describe a requirement no model output can ever satisfy.
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import yaml

from ..errors import ConfigError

# The entity types the intent schema can carry (neuron-intent-decision.schema.json).
REGISTERED_ENTITY_TYPES = frozenset(
    {
        "account_name",
        "policy_number",
        "renewal_id",
        "submission_number",
        "broker_name",
        "task_id",
        "date_expression",
        "context_reference",
    }
)

# Policy ceiling on how many actions one message may resolve to. The schema caps the
# array at 4; this is the routing policy applied after validation (§11.5 rule 7).
MAX_ACTIONS_PER_MESSAGE = 4


class CatalogError(ConfigError):
    """The intent catalog is missing, malformed, or internally inconsistent."""

    title = "Invalid intent catalog"


@dataclass(frozen=True)
class EntityRequirement:
    """One required-entity rule: satisfied when **any** listed entity is present."""

    one_of: tuple[str, ...]

    def satisfied_by(self, entities: dict[str, Any]) -> bool:
        return any((entities or {}).get(name) for name in self.one_of)


@dataclass(frozen=True)
class CatalogAction:
    action_id: str
    domain: str
    active: bool
    description: str = ""
    requires_explicit_confirmation: bool = False
    required_entities: tuple[EntityRequirement, ...] = ()

    def missing_entities(self, entities: dict[str, Any]) -> list[EntityRequirement]:
        """Requirements this entity set does not satisfy (drives a clarify, not a guess)."""
        return [req for req in self.required_entities if not req.satisfied_by(entities)]


@dataclass(frozen=True)
class CatalogDomain:
    domain_id: str
    target_head_card_id: str
    active: bool
    description: str = ""
    actions: dict[str, CatalogAction] = field(default_factory=dict)


@dataclass(frozen=True)
class IntentCatalog:
    catalog_version: str
    domains: dict[str, CatalogDomain]
    content_hash: str

    # --- lookups -------------------------------------------------------------

    def domain(self, domain_id: str | None) -> CatalogDomain | None:
        if not domain_id:
            return None
        return self.domains.get(domain_id)

    def action(self, action_id: str | None) -> CatalogAction | None:
        if not action_id:
            return None
        for domain in self.domains.values():
            found = domain.actions.get(action_id)
            if found is not None:
                return found
        return None

    def head_for(self, domain_id: str) -> str | None:
        """Resolve the specialist head from the **catalog**, never from model output."""
        domain = self.domains.get(domain_id)
        return domain.target_head_card_id if domain else None

    def active_domain_ids(self) -> list[str]:
        return sorted(d.domain_id for d in self.domains.values() if d.active)

    def active_action_ids(self) -> list[str]:
        return sorted(
            action.action_id
            for domain in self.domains.values()
            if domain.active
            for action in domain.actions.values()
            if action.active
        )

    def describe_for_prompt(self) -> str:
        """A compact, deterministic rendering of the active surface for the prompt.

        Deterministic ordering matters: the prompt text feeds a content hash recorded in
        provenance, so an unordered dump would make identical catalogs look different.
        """
        lines: list[str] = []
        for domain_id in self.active_domain_ids():
            domain = self.domains[domain_id]
            lines.append(f"- {domain_id}: {domain.description}".rstrip())
            for action in sorted(domain.actions.values(), key=lambda a: a.action_id):
                if not action.active:
                    continue
                required = ""
                if action.required_entities:
                    groups = [
                        "/".join(req.one_of) for req in action.required_entities
                    ]
                    required = f" (requires {', '.join(groups)})"
                lines.append(f"    - {action.action_id}: {action.description}{required}")
        return "\n".join(lines)


def load_catalog(path: Path, *, registered_head_ids: set[str] | None = None) -> IntentCatalog:
    """Load, validate, and cross-check the catalog. Raises ``CatalogError`` on any fault.

    ``registered_head_ids`` cross-checks each domain against the Agent Card registry; a
    domain pointing at a head that does not exist would route into nothing at runtime,
    so it is refused at startup instead.
    """
    try:
        raw_text = Path(path).read_text(encoding="utf-8")
    except OSError as exc:
        raise CatalogError(f"intent catalog not readable at {path}") from exc
    try:
        data = yaml.safe_load(raw_text) or {}
    except yaml.YAMLError as exc:
        raise CatalogError(f"intent catalog at {path} is not valid YAML") from exc
    if not isinstance(data, dict):
        raise CatalogError("intent catalog must be a mapping")

    catalog_version = str(data.get("catalog_version") or "").strip()
    if not catalog_version:
        raise CatalogError("intent catalog is missing catalog_version")

    raw_domains = data.get("domains")
    if not isinstance(raw_domains, dict) or not raw_domains:
        raise CatalogError("intent catalog must declare at least one domain")

    domains: dict[str, CatalogDomain] = {}
    seen_action_ids: set[str] = set()

    for domain_id, raw_domain in raw_domains.items():
        if not isinstance(raw_domain, dict):
            raise CatalogError(f"domain {domain_id!r} must be a mapping")

        head_id = str(raw_domain.get("target_head_card_id") or "").strip()
        if not head_id:
            raise CatalogError(f"domain {domain_id!r} is missing target_head_card_id")
        if registered_head_ids is not None and head_id not in registered_head_ids:
            raise CatalogError(
                f"domain {domain_id!r} targets unregistered head card {head_id!r}"
            )

        domain_active = bool(raw_domain.get("active", False))
        raw_actions = raw_domain.get("actions") or {}
        if not isinstance(raw_actions, dict):
            raise CatalogError(f"domain {domain_id!r} actions must be a mapping")

        actions: dict[str, CatalogAction] = {}
        for action_id, raw_action in raw_actions.items():
            if not isinstance(raw_action, dict):
                raise CatalogError(f"action {action_id!r} must be a mapping")
            if action_id in seen_action_ids:
                raise CatalogError(f"duplicate action id {action_id!r}")
            seen_action_ids.add(action_id)

            # The prefix rule keeps an action id self-describing: `renewals.view` can
            # never be filed under `tasks`, so a mis-scoped action is caught at load.
            if not action_id.startswith(f"{domain_id}."):
                raise CatalogError(
                    f"action {action_id!r} must be prefixed with its domain {domain_id!r}"
                )

            action_active = bool(raw_action.get("active", False))
            if action_active and not domain_active:
                # An active action under an inactive domain would be executable via a
                # domain that is meant to be dark.
                raise CatalogError(
                    f"action {action_id!r} is active but its domain {domain_id!r} is not"
                )

            requirements: list[EntityRequirement] = []
            for raw_req in raw_action.get("required_entities") or []:
                if not isinstance(raw_req, dict) or "one_of" not in raw_req:
                    raise CatalogError(
                        f"action {action_id!r} required_entities entries must be {{one_of: [...]}}"
                    )
                names = tuple(str(n) for n in (raw_req.get("one_of") or []))
                if not names:
                    raise CatalogError(f"action {action_id!r} has an empty one_of group")
                unknown = set(names) - REGISTERED_ENTITY_TYPES
                if unknown:
                    raise CatalogError(
                        f"action {action_id!r} references unregistered entity types "
                        f"{sorted(unknown)}"
                    )
                requirements.append(EntityRequirement(one_of=names))

            actions[action_id] = CatalogAction(
                action_id=action_id,
                domain=domain_id,
                active=action_active,
                description=str(raw_action.get("description") or ""),
                requires_explicit_confirmation=bool(
                    raw_action.get("requires_explicit_confirmation", False)
                ),
                required_entities=tuple(requirements),
            )

        if domain_active and not any(a.active for a in actions.values()):
            raise CatalogError(
                f"domain {domain_id!r} is active but has no active action to route to"
            )

        domains[domain_id] = CatalogDomain(
            domain_id=domain_id,
            target_head_card_id=head_id,
            active=domain_active,
            description=str(raw_domain.get("description") or ""),
            actions=actions,
        )

    if not any(d.active for d in domains.values()):
        raise CatalogError("intent catalog has no active domain")

    return IntentCatalog(
        catalog_version=catalog_version,
        domains=domains,
        content_hash="sha256:" + hashlib.sha256(raw_text.encode("utf-8")).hexdigest(),
    )
