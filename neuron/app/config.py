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


def _configured_default_provider() -> str:
    """Default model provider from config/models.yaml (the env var still wins)."""
    try:
        data = yaml.safe_load(_MODELS_CONFIG.read_text(encoding="utf-8")) or {}
    except (OSError, yaml.YAMLError):
        return "mock"
    return str(data.get("default_provider", "mock"))


@dataclass(frozen=True)
class Settings:
    engine_base_url: str
    model_provider: str
    request_timeout_s: float
    persistence_backend: str
    cards_dir: Path
    plans_dir: Path
    env: str


def load_settings() -> Settings:
    return Settings(
        engine_base_url=os.environ.get("NEURON_ENGINE_BASE_URL", "http://localhost:8080"),
        model_provider=os.environ.get("NEURON_MODEL_PROVIDER", _configured_default_provider()),
        request_timeout_s=float(os.environ.get("NEURON_REQUEST_TIMEOUT", "10")),
        persistence_backend=os.environ.get("NEURON_PERSISTENCE", "memory"),
        cards_dir=Path(os.environ.get("NEURON_CARDS_DIR", NEURON_ROOT / "crm_agents" / "cards")),
        plans_dir=Path(os.environ.get("NEURON_PLANS_DIR", NEURON_ROOT / "orchestration" / "plans")),
        env=os.environ.get("NEURON_ENV", "development"),
    )
