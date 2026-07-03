"""Fail-fast runtime assembly (F0038-S0001).

``build_runtime`` loads and validates every orchestration asset and wires the
registries, operation store, engine client, and model router. Any invalid or
missing asset raises a ``ConfigError`` so the service refuses to start rather than
serve a half-configured runtime (F0038-S0001 edge cases).
"""

from __future__ import annotations

from .config import Settings, load_settings
from .engine_client import EngineClient
from .errors import ConfigError
from .models.mock_provider import MockProvider
from .models.router import ModelProvider, ModelRouter
from .orchestration.agent_card import AgentCard, load_cards
from .orchestration.heads import BootstrapHandler
from .orchestration.plan import load_plans
from .orchestration.registries import AgentRegistry, ToolRegistry
from .orchestration.task_manager import A2ATaskManager
from .orchestration.zone_heads import RenewalsZoneHead, StubZoneHead
from .persistence.in_memory import InMemoryNeuronRepository
from .persistence.repository import NeuronRepository
from .runtime import NeuronRuntime
from .scope_guard import IntentClassifierHandler, ScopeGuardHandler
from .tools.engine_tools import build_engine_tools


def _pending_story(card: AgentCard) -> str:
    """Which F0038 slice delivers this card's behavioral handler (placeholder until then).

    The orchestrator (glance assembly) and the goal_agent (outreach drafter) keep
    placeholders because their behavior is orchestrated by the GlanceAssembler /
    ActionDispatcher respectively, not invoked through the card handler.
    """
    if card.kind == "orchestrator":
        return "F0038-S0002"
    if card.kind == "goal_agent":
        return "F0038-S0005"
    if card.kind == "specialist_head":
        return "F0038-S0003" if card.active else "F0038-S0004"
    return "F0038"


def _make_handler(card: AgentCard):
    """Bind a card to its handler. Specialist heads get zone-producing handlers
    (F0038-S0002/S0004); the live Renewals head is F0038-S0003. The scope guard and
    intent classifier get behavioral handlers (F0038-S0007). The orchestrator and
    outreach drafter keep placeholders — their behavior lives in the GlanceAssembler /
    ActionDispatcher, not the card handler."""
    if card.card_id == "crm.renewals.head":
        return RenewalsZoneHead(card)
    if card.kind == "specialist_head":
        return StubZoneHead(card)
    if card.kind == "scope_guard":
        return ScopeGuardHandler(card)
    if card.kind == "intent_classifier":
        return IntentClassifierHandler(card)
    return BootstrapHandler(card, _pending_story(card))


def _build_repository(backend: str) -> NeuronRepository:
    if backend == "memory":
        return InMemoryNeuronRepository()
    # WHY: F0038 ships only the in-memory store behind the interface (ADR-028 §1);
    # the durable Postgres impl (migrations/0001) is wired in a later feature.
    raise ConfigError(f"unsupported persistence backend {backend!r} (F0038 supports 'memory')")


def _build_model_router(provider_name: str) -> ModelRouter:
    providers: dict[str, ModelProvider] = {"mock": MockProvider()}
    if provider_name not in providers:
        raise ConfigError(
            f"model provider {provider_name!r} is not wired in F0038 (available: {sorted(providers)})"
        )
    return ModelRouter(providers, default=provider_name)


def build_runtime(settings: Settings | None = None) -> NeuronRuntime:
    settings = settings or load_settings()

    # 1. Agent Cards → registry (zone heads for specialist heads; placeholders otherwise).
    agents = AgentRegistry()
    for card in load_cards(settings.cards_dir).values():
        agents.register(card, _make_handler(card))

    # 2. Engine client + tool registry.
    engine_client = EngineClient(settings.engine_base_url, timeout=settings.request_timeout_s)
    tools = ToolRegistry()
    tools.register_all(build_engine_tools(engine_client))

    # 3. Plans — validated against the schema AND cross-checked against registries.
    plans = load_plans(settings.plans_dir, agents, tools)

    # 4. Store, model router, task manager.
    repository = _build_repository(settings.persistence_backend)
    model_router = _build_model_router(settings.model_provider)
    task_manager = A2ATaskManager(repository)

    return NeuronRuntime(
        settings=settings,
        agents=agents,
        tools=tools,
        plans=plans,
        repository=repository,
        engine_client=engine_client,
        model_router=model_router,
        task_manager=task_manager,
    )
