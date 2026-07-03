"""The assembled, validated Neuron runtime (F0038-S0001).

Holds the registries, validated plans, operation store, engine client, model
router, and task manager. It carries no per-request business state — the service is
stateless; durable operation state lives in the repository (ADR-028 §1).
"""

from __future__ import annotations

from dataclasses import dataclass

from .config import Settings
from .engine_client import EngineClient
from .models.router import ModelRouter
from .orchestration.plan import OrchestrationPlan
from .orchestration.registries import AgentRegistry, ToolRegistry
from .orchestration.task_manager import A2ATaskManager
from .persistence.repository import NeuronRepository


@dataclass
class NeuronRuntime:
    settings: Settings
    agents: AgentRegistry
    tools: ToolRegistry
    plans: dict[str, OrchestrationPlan]
    repository: NeuronRepository
    engine_client: EngineClient
    model_router: ModelRouter
    task_manager: A2ATaskManager

    def health_snapshot(self) -> dict:
        """Payload for GET /health (neuron-api.yaml): status + registered heads/tools."""
        return {
            "status": "healthy",
            "heads": self.agents.heads(),
            "tools": self.tools.names(),
        }

    def readiness(self) -> tuple[bool, dict]:
        """Readiness: at least one validated plan, an operation store, a default provider."""
        detail = {
            "plans": sorted(self.plans),
            "agents": self.agents.card_ids(),
            "model_provider": self.model_router.default,
            "persistence": self.settings.persistence_backend,
        }
        ready = bool(self.plans) and self.repository is not None
        return ready, detail
