"""Runtime settings (env-driven, with F0038 defaults).

No secrets are stored here; the forwarded user token is per-request and the mock
provider needs no key. ``NEURON_MODEL_PROVIDER`` (env) wins; otherwise the default
provider comes from config/models.yaml, falling back to ``mock`` for this run.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

import yaml

# neuron/ root (this file is neuron/app/config.py).
NEURON_ROOT = Path(__file__).resolve().parents[1]
_MODELS_CONFIG = NEURON_ROOT / "config" / "models.yaml"


def load_model_config() -> dict:
    """Parsed config/models.yaml. Returns {} when unreadable — callers fail fast on the
    specific field they need, which gives a better message than a parse error here."""
    try:
        return yaml.safe_load(_MODELS_CONFIG.read_text(encoding="utf-8")) or {}
    except (OSError, yaml.YAMLError):
        return {}


def _configured_default_provider() -> str:
    """Default model provider from config/models.yaml (the env var still wins)."""
    return str(load_model_config().get("default_provider", "mock"))


@dataclass(frozen=True)
class Settings:
    engine_base_url: str
    model_provider: str
    request_timeout_s: float
    persistence_backend: str
    cards_dir: Path
    plans_dir: Path
    env: str
    # F0039-S0001 — durable store. The DSN is read from the environment and is never
    # written to logs or provenance; it is the only place a credential appears.
    postgres_dsn: str
    postgres_pool_min: int
    postgres_pool_max: int
    # F0039-S0005 — trusted intent registry + versioned prompt assets.
    intent_catalog_path: Path
    prompts_dir: Path
    # F0039-S0007/S0008 — routing mode: `direct` (Phi decides), `shadow` (deterministic
    # decides, Phi recorded only), or `deterministic` (the tested rollback path, spec §33).
    intent_mode: str


def load_settings() -> Settings:
    return Settings(
        engine_base_url=os.environ.get("NEURON_ENGINE_BASE_URL", "http://localhost:8080"),
        model_provider=os.environ.get("NEURON_MODEL_PROVIDER", _configured_default_provider()),
        request_timeout_s=float(os.environ.get("NEURON_REQUEST_TIMEOUT", "10")),
        persistence_backend=os.environ.get("NEURON_PERSISTENCE", "memory"),
        cards_dir=Path(os.environ.get("NEURON_CARDS_DIR", NEURON_ROOT / "crm_agents" / "cards")),
        plans_dir=Path(os.environ.get("NEURON_PLANS_DIR", NEURON_ROOT / "orchestration" / "plans")),
        env=os.environ.get("NEURON_ENV", "development"),
        postgres_dsn=os.environ.get("NEURON_POSTGRES_DSN", ""),
        # Bounded on purpose: an unbounded pool turns a traffic spike into database
        # exhaustion for every other service sharing the engine database.
        postgres_pool_min=int(os.environ.get("NEURON_POSTGRES_POOL_MIN", "1")),
        postgres_pool_max=int(os.environ.get("NEURON_POSTGRES_POOL_MAX", "10")),
        intent_catalog_path=Path(
            os.environ.get("NEURON_INTENT_CATALOG", NEURON_ROOT / "config" / "intent-catalog.yaml")
        ),
        prompts_dir=Path(os.environ.get("NEURON_PROMPTS_DIR", NEURON_ROOT / "prompts")),
        # Default is SHADOW, not direct: spec §33 enables direct routing only after the
        # §30.4 gates pass, and the 2026-07-25 evaluation run is red (see
        # evals/reports/). Flipping this default is a rollout decision backed by a green
        # report, not a code change someone makes in passing.
        intent_mode=os.environ.get("NEURON_INTENT_MODE", "shadow"),
    )
