"""Versioned YAML orchestration plans (ADR-027 §3, neuron-orchestration-plan.schema.json).

Loaded and validated at startup. A plan is refused (fail-fast) when it fails the
JSON Schema, references an unregistered head/tool, points a transition at an
unknown step/terminal-state, or omits ``on_failure`` on a step that calls the
engine or a model — "no silent fallthrough" (F0038-S0001).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import jsonschema
import yaml

from ..errors import ConfigError, PlanValidationError, UnknownReferenceError
from ..schemas import get_validator
from .registries import AgentRegistry, ToolRegistry

# Agent kinds whose step performs an engine or model call, so it must declare a
# fallback path (schema note on ``on_failure``).
_FALLBACK_REQUIRED_KINDS = {
    "specialist_head",
    "goal_agent",
    "intent_classifier",
    "scope_guard",
}


@dataclass(frozen=True)
class PlanStep:
    step_id: str
    agent: str
    skill_id: str | None = None
    accepted_output_modes: tuple[str, ...] = ()
    tools: tuple[str, ...] = ()
    on_success: str | None = None
    on_failure: str | None = None
    is_terminal: bool = False


@dataclass(frozen=True)
class OrchestrationPlan:
    plan_id: str
    plan_version: str
    entrypoint: str
    terminal_states: tuple[str, ...]
    steps: tuple[PlanStep, ...]
    description: str | None = None
    trace: dict[str, Any] = field(default_factory=dict)
    raw: dict[str, Any] = field(default_factory=dict, repr=False)

    def step(self, step_id: str) -> PlanStep:
        for s in self.steps:
            if s.step_id == step_id:
                return s
        raise KeyError(step_id)


def validate_plan(
    data: dict[str, Any],
    agents: AgentRegistry,
    tools: ToolRegistry,
    *,
    source: str | None = None,
) -> OrchestrationPlan:
    where = f" ({source})" if source else ""
    try:
        get_validator("orchestration-plan").validate(data)
    except jsonschema.ValidationError as exc:
        raise PlanValidationError(f"plan failed schema{where}: {exc.message}") from exc

    steps = [
        PlanStep(
            step_id=s["step_id"],
            agent=s["agent"],
            skill_id=s.get("skill_id"),
            accepted_output_modes=tuple(s.get("accepted_output_modes", [])),
            tools=tuple(s.get("tools", [])),
            on_success=s.get("on_success"),
            on_failure=s.get("on_failure"),
            is_terminal=s.get("is_terminal", False),
        )
        for s in data["steps"]
    ]

    step_ids = {s.step_id for s in steps}
    if len(step_ids) != len(steps):
        raise PlanValidationError(f"duplicate step_id in plan {data['plan_id']!r}{where}")

    terminal_states = set(data["terminal_states"])
    valid_targets = step_ids | terminal_states

    if data["entrypoint"] not in step_ids:
        raise PlanValidationError(
            f"entrypoint {data['entrypoint']!r} is not a step in plan {data['plan_id']!r}{where}"
        )

    for step in steps:
        # Every referenced head/agent must resolve in the registry.
        if not agents.has(step.agent):
            raise UnknownReferenceError(
                f"plan {data['plan_id']!r} step {step.step_id!r} references "
                f"unregistered agent {step.agent!r}{where}"
            )
        # Every referenced tool must resolve in the tool registry.
        for tool in step.tools:
            if not tools.has(tool):
                raise UnknownReferenceError(
                    f"plan {data['plan_id']!r} step {step.step_id!r} references "
                    f"unregistered tool {tool!r}{where}"
                )
        # Transitions must land on a known step or declared terminal state.
        for label, target in (("on_success", step.on_success), ("on_failure", step.on_failure)):
            if target is not None and target not in valid_targets:
                raise PlanValidationError(
                    f"plan {data['plan_id']!r} step {step.step_id!r} {label} "
                    f"target {target!r} is neither a step nor a terminal_state{where}"
                )
        # No silent fallthrough: engine/model steps must declare a failure path.
        if not step.is_terminal:
            calls_engine_or_model = bool(step.tools) or (
                agents.get(step.agent).card.kind in _FALLBACK_REQUIRED_KINDS
            )
            if calls_engine_or_model and step.on_failure is None:
                raise PlanValidationError(
                    f"plan {data['plan_id']!r} step {step.step_id!r} calls the engine/model "
                    f"but declares no on_failure (no silent fallthrough){where}"
                )

    return OrchestrationPlan(
        plan_id=data["plan_id"],
        plan_version=data["plan_version"],
        entrypoint=data["entrypoint"],
        terminal_states=tuple(data["terminal_states"]),
        steps=tuple(steps),
        description=data.get("description"),
        trace=data.get("trace", {}),
        raw=data,
    )


def load_plans(
    plans_dir: str | Path, agents: AgentRegistry, tools: ToolRegistry
) -> dict[str, OrchestrationPlan]:
    """Load + validate every ``*.plan.yaml`` under ``plans_dir`` (fail-fast)."""
    plans_dir = Path(plans_dir)
    if not plans_dir.is_dir():
        raise ConfigError(f"orchestration plans directory not found: {plans_dir}")
    plans: dict[str, OrchestrationPlan] = {}
    for path in sorted(plans_dir.glob("*.plan.yaml")):
        try:
            data = yaml.safe_load(path.read_text(encoding="utf-8"))
        except yaml.YAMLError as exc:
            raise PlanValidationError(f"unparseable plan {path.name}: {exc}") from exc
        if not isinstance(data, dict):
            raise PlanValidationError(f"plan {path.name} is not a mapping")
        plan = validate_plan(data, agents, tools, source=path.name)
        if plan.plan_id in plans:
            raise PlanValidationError(f"duplicate plan_id {plan.plan_id!r} in {path.name}")
        plans[plan.plan_id] = plan
    if not plans:
        raise ConfigError(f"no orchestration plans found in {plans_dir}")
    return plans
