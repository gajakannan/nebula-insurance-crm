"""MCP-shaped engine tool descriptors (F0038-S0001).

Each descriptor names an allow-listed engine endpoint a head may call. ``invoke``
forwards the user token via the :class:`EngineClient`; the engine enforces Casbin.
Path parameters (e.g. ``renewalId``) are substituted from ``path_params``. The
concrete head wiring that calls these is F0038-S0003/S0005/S0006.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from ..engine_client import EngineClient


@dataclass(frozen=True)
class EngineToolDescriptor:
    name: str
    method: str
    path_template: str
    description: str
    auth_mode: str = "user_token"
    _client: EngineClient | None = field(default=None, repr=False, compare=False)

    async def invoke(
        self,
        *,
        user_token: str,
        path_params: dict[str, Any] | None = None,
        json: Any | None = None,
        params: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
    ) -> Any:
        if self._client is None:
            raise RuntimeError(f"tool {self.name!r} was registered without an engine client")
        path = self.path_template.format(**(path_params or {}))
        return await self._client.call(
            self.method, path, user_token=user_token, json=json, params=params, headers=headers
        )


def build_engine_tools(client: EngineClient) -> list[EngineToolDescriptor]:
    """The five engine tools F0038 exposes to specialist heads (assembly plan Step 1)."""
    specs = [
        (
            "engine.renewals.needs_attention",
            "GET",
            "/renewals/needs-attention",
            "List renewals needing attention (Identified/Outreach, expiry <=90d).",
        ),
        (
            "engine.renewals.companion_context",
            "GET",
            "/renewals/{renewalId}/companion-context",
            "Per-renewal drill context for the companion.",
        ),
        (
            "engine.renewals.outreach_draft",
            "POST",
            "/renewals/{renewalId}/outreach-draft",
            "Persist an AI outreach draft as a renewal ActivityTimelineEvent (no transition).",
        ),
        (
            "engine.renewals.outreach_mock_send",
            "POST",
            "/renewals/{renewalId}/outreach-mock-send",
            "Commit Identified->Outreach + 'sent (simulated)' event (no SMTP).",
        ),
        (
            "engine.telemetry.ingest",
            "POST",
            "/internal/telemetry/neuron-companion",
            "Ingest a companion telemetry event.",
        ),
    ]
    return [
        EngineToolDescriptor(
            name=name,
            method=method,
            path_template=path,
            description=description,
            _client=client,
        )
        for name, method, path, description in specs
    ]
