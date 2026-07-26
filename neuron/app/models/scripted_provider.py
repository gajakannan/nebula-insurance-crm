"""Scripted provider — test case in, controlled result out (F0039-S0004).

The evaluation harness (S0008) and the resolver's regression fixtures (S0006) need to
drive *exact* model behaviour, including the failure modes: a malformed object, a
scope-escape attempt, a timeout. `MockProvider` is deterministic but not steerable, and
the live Phi provider is neither. This one is scripted.

Scripts are keyed by the prompt's content hash, so a fixture binds to the exact prompt
text it was written against — if a prompt changes, its script stops matching instead of
silently answering a different question.
"""

from __future__ import annotations

from typing import Any

from .errors import ProviderConfigError
from .router import (
    ModelProvenance,
    ModelResult,
    StructuredModelResult,
    content_hash,
    enforce_budget,
    parse_structured_content,
)

_MODEL_NAME = "scripted-1"


class ScriptedProvider:
    """Returns pre-registered results; raises pre-registered errors.

    Register with :meth:`script` (a dict to return) or :meth:`script_error` (an
    exception to raise). Unscripted prompts raise ``ProviderConfigError`` — a silent
    default would let a test pass while exercising nothing.
    """

    name = "scripted"

    def __init__(self, *, default: dict[str, Any] | None = None) -> None:
        self._by_hash: dict[str, dict[str, Any]] = {}
        self._errors: dict[str, BaseException] = {}
        self._default = default
        self.calls: list[dict[str, Any]] = []

    # --- scripting -----------------------------------------------------------

    def script(self, prompt: str, result: dict[str, Any]) -> "ScriptedProvider":
        self._by_hash[content_hash(prompt)] = result
        return self

    def script_error(self, prompt: str, error: BaseException) -> "ScriptedProvider":
        self._errors[content_hash(prompt)] = error
        return self

    def script_default(self, result: dict[str, Any]) -> "ScriptedProvider":
        self._default = result
        return self

    # --- provider contract ---------------------------------------------------

    def complete(
        self, prompt: str, *, system: str | None = None, max_tokens: int = 1024
    ) -> ModelResult:
        data = self._lookup(prompt)
        import json

        text = json.dumps(data, sort_keys=True)
        return ModelResult(
            model=_MODEL_NAME,
            content=text,
            content_hash=content_hash(text),
            prompt_tokens=len(prompt.split()),
            completion_tokens=len(text.split()),
        )

    async def complete_structured(
        self,
        *,
        prompt: str,
        schema: dict[str, Any],
        schema_name: str = "decision",
        system: str | None = None,
        max_tokens: int = 512,
    ) -> StructuredModelResult:
        # Budgeting is enforced here too, so a fixture cannot accidentally pass a
        # request the live provider would reject.
        enforce_budget(system=system, prompt=prompt, max_tokens=max_tokens)
        self.calls.append({"prompt": prompt, "schema_name": schema_name, "system": system})

        key = content_hash(prompt)
        if key in self._errors:
            raise self._errors[key]

        data = self._lookup(prompt)
        import json

        # Round-trip through the same parser the live provider uses, so a fixture that
        # scripts a non-object fails exactly the way production would.
        text = data if isinstance(data, str) else json.dumps(data, sort_keys=True)
        parsed = parse_structured_content(text)
        return StructuredModelResult(
            data=parsed,
            provenance=ModelProvenance(
                model=_MODEL_NAME,
                content_hash=content_hash(text),
                prompt_tokens=len(prompt.split()),
                completion_tokens=len(text.split()),
                total_tokens=len(prompt.split()) + len(text.split()),
                latency_ms=0,
                model_revision="scripted",
            ),
        )

    def _lookup(self, prompt: str) -> Any:
        key = content_hash(prompt)
        if key in self._by_hash:
            return self._by_hash[key]
        if self._default is not None:
            return self._default
        raise ProviderConfigError(
            "scripted provider has no script for this prompt (and no default)"
        )
