"""Engine client — calls the .NET engine **as the user** (ADR-027 §8, F0038-S0001).

Every engine call forwards the caller's authentik bearer token (on-behalf-of); the
engine enforces Casbin ABAC. Neuron re-implements no authorization. An unreachable
engine surfaces as a typed ``UpstreamUnavailableError`` (502), never a raw stack
trace/500 leak. The bearer token is never logged.

Testable without ``httpx``: the transport is an injectable ``sender`` coroutine;
the default lazily constructs an ``httpx.AsyncClient`` only when actually used.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Awaitable, Callable

from .errors import UpstreamAuthError, UpstreamUnavailableError

# Transport-level failures that mean "engine unreachable". httpx errors are added
# when httpx is installed; the stdlib set keeps the module importable (and the
# injected-sender tests runnable) without httpx.
_TRANSPORT_ERRORS: tuple[type[BaseException], ...] = (ConnectionError, TimeoutError, OSError)
try:  # pragma: no cover - depends on optional runtime dep
    import httpx

    _TRANSPORT_ERRORS = (*_TRANSPORT_ERRORS, httpx.TransportError)
except ImportError:  # pragma: no cover
    httpx = None  # type: ignore[assignment]


@dataclass(frozen=True)
class EngineResponse:
    status_code: int
    body: Any

    def json(self) -> Any:
        return self.body


Sender = Callable[..., Awaitable[EngineResponse]]


class EngineClient:
    def __init__(
        self,
        base_url: str,
        *,
        timeout: float = 10.0,
        sender: Sender | None = None,
    ) -> None:
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout
        self._sender = sender  # injected transport for tests; None → lazy httpx

    async def call(
        self,
        method: str,
        path: str,
        *,
        user_token: str,
        json: Any | None = None,
        params: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
    ) -> Any:
        url = f"{self._base_url}/{path.lstrip('/')}"
        # WHY: forwarded end-user token is the ONLY authorization — the engine decides.
        request_headers = {"Authorization": f"Bearer {user_token}", "Accept": "application/json"}
        if headers:
            request_headers.update(headers)
        send = self._sender or self._httpx_send
        try:
            response = await send(method=method, url=url, headers=request_headers, json=json, params=params)
        except _TRANSPORT_ERRORS as exc:
            raise UpstreamUnavailableError(f"engine unreachable: {type(exc).__name__}") from exc

        if response.status_code in (401, 403):
            raise UpstreamAuthError(response.status_code, "engine rejected forwarded token")
        if response.status_code >= 500:
            raise UpstreamUnavailableError(f"engine returned {response.status_code}")
        return response.json()

    async def _httpx_send(
        self,
        *,
        method: str,
        url: str,
        headers: dict[str, str],
        json: Any | None,
        params: dict[str, Any] | None,
    ) -> EngineResponse:  # pragma: no cover - exercised in integration/runtime, not unit
        if httpx is None:
            raise UpstreamUnavailableError("httpx is not installed in this runtime")
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.request(method, url, headers=headers, json=json, params=params)
            try:
                body = resp.json()
            except ValueError:
                body = None
            return EngineResponse(status_code=resp.status_code, body=body)
