"""Versioned prompt registry (F0039-S0005, spec §10.5/§11.6).

Prompts are **checked-in, versioned assets** with recorded content hashes — not strings
built at runtime. That is what makes a past decision explainable: a provenance row names
the prompt id, its version, and the hash of the exact text that produced the output, so
the prompt can be recovered later and the decision reproduced.

Layout::

    neuron/prompts/<prompt-id>/<version>/system.md
    neuron/prompts/<prompt-id>/<version>/metadata.yaml

Loading is fail-fast: a missing or empty asset raises `ConfigError` at startup, matching
Agent Card / plan / catalog behaviour. A resolver that silently ran without its system
prompt would be a security problem, not a degraded feature.
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass
from pathlib import Path

import yaml

from ..errors import ConfigError

# Prompt ids this runtime expects. `crm-intent-resolver` is the composed single-call
# prompt (§11.8); the two fragments remain separately versioned so a scope-only or
# classifier-only change does not invalidate the composed prompt's provenance.
SCOPE_GUARD_PROMPT = "crm-scope-guard"
INTENT_CLASSIFIER_PROMPT = "crm-intent-classifier"
INTENT_RESOLVER_PROMPT = "crm-intent-resolver"
INTENT_ADJUDICATOR_PROMPT = "crm-intent-adjudicator"


class PromptError(ConfigError):
    """A prompt asset is missing, empty, or malformed."""

    title = "Invalid prompt asset"


@dataclass(frozen=True)
class Prompt:
    prompt_id: str
    version: str
    text: str
    content_hash: str
    metadata: dict

    @property
    def reference(self) -> str:
        """`id@version` — what provenance records alongside the hash."""
        return f"{self.prompt_id}@{self.version}"


class PromptRegistry:
    """Loads and caches versioned prompt assets from a root directory."""

    def __init__(self, root: Path) -> None:
        self._root = Path(root)
        self._cache: dict[tuple[str, str], Prompt] = {}

    def load(self, prompt_id: str, version: str = "1.0.0") -> Prompt:
        key = (prompt_id, version)
        if key in self._cache:
            return self._cache[key]

        directory = self._root / prompt_id / version
        system_path = directory / "system.md"
        try:
            text = system_path.read_text(encoding="utf-8")
        except OSError as exc:
            raise PromptError(
                f"prompt asset {prompt_id}@{version} not found at {system_path}"
            ) from exc
        if not text.strip():
            # An empty system prompt would silently remove every instruction the
            # resolver depends on — refuse rather than run unguided.
            raise PromptError(f"prompt asset {prompt_id}@{version} is empty")

        metadata: dict = {}
        metadata_path = directory / "metadata.yaml"
        if metadata_path.exists():
            try:
                metadata = yaml.safe_load(metadata_path.read_text(encoding="utf-8")) or {}
            except yaml.YAMLError as exc:
                raise PromptError(
                    f"prompt metadata for {prompt_id}@{version} is not valid YAML"
                ) from exc
            if not isinstance(metadata, dict):
                raise PromptError(f"prompt metadata for {prompt_id}@{version} must be a mapping")

        declared = str(metadata.get("version") or version)
        if declared != version:
            # A metadata version that disagrees with its directory makes provenance
            # ambiguous — which one did we actually run?
            raise PromptError(
                f"prompt {prompt_id} metadata declares version {declared!r} "
                f"but lives in {version!r}"
            )

        prompt = Prompt(
            prompt_id=prompt_id,
            version=version,
            text=text,
            content_hash="sha256:" + hashlib.sha256(text.encode("utf-8")).hexdigest(),
            metadata=metadata,
        )
        self._cache[key] = prompt
        return prompt

    def load_all(self, prompt_ids: list[str], version: str = "1.0.0") -> dict[str, Prompt]:
        """Load every required prompt, so a missing asset fails at startup not mid-turn."""
        return {prompt_id: self.load(prompt_id, version) for prompt_id in prompt_ids}
