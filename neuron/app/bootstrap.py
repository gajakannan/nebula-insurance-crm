"""Fail-fast runtime assembly (F0038-S0001).

``build_runtime`` loads and validates every orchestration asset and wires the
registries, operation store, engine client, and model router. Any invalid or
missing asset raises a ``ConfigError`` so the service refuses to start rather than
serve a half-configured runtime (F0038-S0001 edge cases).
"""

from __future__ import annotations

import os
from collections.abc import Callable

from .components import COMPONENTS
from .config import Settings, load_model_config, load_settings
from .engine_client import EngineClient
from .errors import ConfigError
from .intent.catalog import load_catalog
from .intent.prompt_registry import (
    INTENT_CLASSIFIER_PROMPT,
    INTENT_RESOLVER_PROMPT,
    SCOPE_GUARD_PROMPT,
    PromptRegistry,
)
from .models.mock_provider import MockProvider
from .models.openai_compatible_provider import OpenAICompatibleProvider, PhiProfile
from .models.router import ModelProvider, ModelRouter
from .models.scripted_provider import ScriptedProvider
from .orchestration.agent_card import AgentCard, load_cards
from .orchestration.heads import BootstrapHandler
from .orchestration.plan import load_plans
from .orchestration.registries import AgentRegistry, ToolRegistry
from .orchestration.task_manager import A2ATaskManager
from .orchestration.zone_heads import BrokerActivityZoneHead, RenewalsZoneHead, StubZoneHead
from .persistence.in_memory import InMemoryNeuronRepository
from .persistence.postgres import PostgresNeuronRepository
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


_LIVE_HEAD_FACTORIES: dict[str, Callable[[AgentCard], object]] = {
    "crm.renewals.head": RenewalsZoneHead,
    "crm.broker_activity.head": BrokerActivityZoneHead,
}


def _make_handler(card: AgentCard):
    """Bind a card to its handler. Specialist heads get zone-producing handlers
    (F0038-S0002/S0004); the live Renewals head is F0038-S0003. The scope guard and
    intent classifier get behavioral handlers (F0038-S0007). The orchestrator and
    outreach drafter keep placeholders — their behavior lives in the GlanceAssembler /
    ActionDispatcher, not the card handler."""
    if card.kind == "specialist_head":
        if card.active:
            factory = _LIVE_HEAD_FACTORIES.get(card.card_id)
            if factory is None:
                raise ConfigError(
                    f"active specialist head {card.card_id!r} has no live handler factory"
                )
            if card.auth_mode != "user_token" and card.tools:
                raise ConfigError(
                    f"active engine head {card.card_id!r} must use auth_mode:user_token"
                )
            return factory(card)
        if card.tools or card.components:
            raise ConfigError(
                f"inactive specialist head {card.card_id!r} may not declare tools/components"
            )
        return StubZoneHead(card)
    if card.kind == "scope_guard":
        return ScopeGuardHandler(card)
    if card.kind == "intent_classifier":
        return IntentClassifierHandler(card)
    return BootstrapHandler(card, _pending_story(card))


def _build_repository(settings: Settings) -> NeuronRepository:
    backend = settings.persistence_backend
    if backend == "memory":
        return InMemoryNeuronRepository()
    if backend == "postgres":
        # F0039-S0001: the durable home. Neuron owns and writes neuron.* directly
        # (ADR-028 §1) — this is a direct Postgres connection, not an engine call.
        if not settings.postgres_dsn:
            raise ConfigError(
                "NEURON_PERSISTENCE=postgres requires NEURON_POSTGRES_DSN "
                "(fail fast rather than start with no durable store)"
            )
        return PostgresNeuronRepository(
            settings.postgres_dsn,
            min_size=settings.postgres_pool_min,
            max_size=settings.postgres_pool_max,
        )
    raise ConfigError(
        f"unsupported persistence backend {backend!r} (supported: 'memory', 'postgres')"
    )


def _phi_profile(config: dict) -> PhiProfile:
    """Build the local Phi profile from config/models.yaml + env (F0039-S0004).

    The API key is read from the environment variable *named* by `api_key_env` — the
    key itself is never in the repo, and a missing key fails fast at startup rather
    than surfacing as a puzzling 401 on the first user message.
    """
    key_env = config.get("api_key_env") or "NEURON_PHI_API_KEY"
    api_key = os.environ.get(key_env, "")
    if not api_key:
        raise ConfigError(
            f"model provider 'local_phi' requires the {key_env} environment variable"
        )
    return PhiProfile(
        base_url=os.environ.get("NEURON_PHI_BASE_URL") or config.get("base_url", ""),
        model=os.environ.get("NEURON_PHI_MODEL") or config.get("model", ""),
        api_key=api_key,
        model_revision=os.environ.get("NEURON_PHI_MODEL_REVISION")
        or config.get("model_revision"),
        image_digest=os.environ.get("NEURON_PHI_IMAGE_DIGEST") or config.get("image_digest"),
        context_limit=int(config.get("context_limit") or 4096),
        max_output_tokens=int(config.get("max_output_tokens") or 512),
        timeout_s=float(config.get("timeout_s") or 30.0),
    )


def _build_model_router(settings: Settings) -> ModelRouter:
    """Register the providers this runtime can serve.

    `mock` and `scripted` are always available (no key, no GPU). `local_phi` is built
    only when it is the selected default — constructing it eagerly would demand a key
    from every developer running on the mock profile.
    """
    provider_name = settings.model_provider
    providers: dict[str, ModelProvider] = {
        "mock": MockProvider(),
        "scripted": ScriptedProvider(),
    }
    if provider_name == "local_phi":
        profiles = (load_model_config().get("providers") or {})
        providers["local_phi"] = OpenAICompatibleProvider(
            _phi_profile(profiles.get("local_phi") or {})
        )
    if provider_name not in providers:
        raise ConfigError(
            f"model provider {provider_name!r} is not wired (available: {sorted(providers)})"
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
    plans = load_plans(settings.plans_dir, agents, tools, COMPONENTS)

    # 4. Intent registry + versioned prompts (F0039-S0005). Loaded before the store so
    #    an invalid catalog or a missing prompt stops startup, exactly like a bad plan.
    intent_catalog = load_catalog(
        settings.intent_catalog_path, registered_head_ids=set(agents.card_ids())
    )
    prompts = PromptRegistry(settings.prompts_dir).load_all(
        [SCOPE_GUARD_PROMPT, INTENT_CLASSIFIER_PROMPT, INTENT_RESOLVER_PROMPT]
    )

    # 5. Store, model router, task manager.
    repository = _build_repository(settings)
    model_router = _build_model_router(settings)
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
        components=COMPONENTS,
        intent_catalog=intent_catalog,
        prompts=prompts,
    )
