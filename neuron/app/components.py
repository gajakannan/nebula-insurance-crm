"""Server-side registered component contracts (F0040 / ADR-037).

Every app-part id resolves to an offline JSON Schema validator. Specialist heads also
declare ownership of the components they may emit; :class:`HeadExecutor` enforces both
ownership and props validation before a payload can reach persistence or React.
"""

from __future__ import annotations

from typing import Any

import jsonschema

from .errors import NeuronError
from .schemas import load_schema


class UnknownComponentError(NeuronError):
    status = 500
    title = "Unregistered component"


class InvalidComponentPropsError(NeuronError):
    status = 500
    title = "Invalid component props"


class ComponentContractRegistry:
    def __init__(self) -> None:
        self._validators: dict[str, jsonschema.protocols.Validator] = {}

    def register(self, component_id: str, schema: dict[str, Any]) -> None:
        if component_id in self._validators:
            raise ValueError(f"component {component_id!r} already registered")
        validator_cls = jsonschema.validators.validator_for(schema)
        validator_cls.check_schema(schema)
        self._validators[component_id] = validator_cls(schema)

    def has(self, component_id: str) -> bool:
        return component_id in self._validators

    def names(self) -> list[str]:
        return sorted(self._validators)

    def validate(self, component_id: str, props: dict[str, Any]) -> None:
        validator = self._validators.get(component_id)
        if validator is None:
            raise UnknownComponentError(f"component {component_id!r} is not registered")
        try:
            validator.validate(props)
        except jsonschema.ValidationError as exc:
            raise InvalidComponentPropsError(
                f"component {component_id!r} props failed validation: {exc.message}"
            ) from exc


def build_component_registry() -> ComponentContractRegistry:
    registry = ComponentContractRegistry()

    # Existing F0038 components retain their shipped frontend contracts. They are
    # registered here so every app part has a validator association even though only
    # the new Broker contract is a separately governed planning schema.
    registry.register(
        "renewals.needs_attention_list",
        {
            "type": "object",
            "required": ["items"],
            "additionalProperties": True,
            "properties": {
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "required": [
                            "renewalId", "accountName", "expiresInDays", "workflowState"
                        ],
                        "additionalProperties": True,
                        "properties": {
                            "renewalId": {"type": "string"},
                            "accountName": {"type": "string"},
                            "expiresInDays": {"type": "integer"},
                            "workflowState": {"type": "string"},
                            "noBrokerContact30d": {"type": "boolean"},
                        },
                    },
                }
            },
        },
    )
    registry.register(
        "renewals.companion_context",
        {
            "type": "object",
            "required": ["renewalId", "accountName", "workflowState"],
            "additionalProperties": True,
            "properties": {
                "renewalId": {"type": "string"},
                "accountName": {"type": "string"},
                "workflowState": {"type": "string"},
            },
        },
    )
    registry.register(
        "outreach.draft_editor",
        {
            "type": "object",
            "required": ["renewalId", "draftBody"],
            "additionalProperties": True,
            "properties": {
                "renewalId": {"type": "string"},
                "draftBody": {"type": "string"},
            },
        },
    )
    registry.register("broker_activity.recent_list", load_schema("broker-activity-list"))
    return registry


COMPONENTS = build_component_registry()
