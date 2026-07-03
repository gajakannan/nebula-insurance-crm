"""FastAPI entrypoint for the Neuron Companion (neuron-api.yaml).

Thin by design: it builds the validated runtime at startup (fail-fast) and exposes
the HTTP surface. ``/health`` + ``/ready`` (S0001), ``GET /v1/glance`` (S0002 zone
assembly), ``POST /v1/actions`` (S0003/S0005/S0006 component callbacks), and
``POST /v1/messages`` (S0007 scope-guarded conversational send) are all live. All
business logic lives in the runtime modules, which are framework-agnostic and
unit-tested without FastAPI.
"""

from __future__ import annotations

from fastapi import Depends, FastAPI, Header, Request
from fastapi.responses import JSONResponse

from .auth import subject_from_token
from .actions import ActionDispatcher
from .bootstrap import build_runtime
from .errors import NeuronError
from .messages import MessageDispatcher
from .orchestration.glance import GlanceAssembler
from .runtime import NeuronRuntime

_PROBLEM_JSON = "application/problem+json"


def _problem(status: int, title: str, detail: str, type_slug: str, instance: str | None = None) -> JSONResponse:
    body = {
        "type": f"https://nebula.local/problems/{type_slug}",
        "title": title,
        "status": status,
        "detail": detail,
    }
    if instance is not None:
        body["instance"] = instance
    return JSONResponse(status_code=status, content=body, media_type=_PROBLEM_JSON)


class _Unauthorized(NeuronError):
    status = 401
    title = "Unauthorized"

    def __init__(self) -> None:
        super().__init__("missing or malformed bearer token")


async def require_bearer(authorization: str | None = Header(default=None)) -> str:
    """Extract the forwarded user token; the engine (not Neuron) authorizes it."""
    if not authorization or not authorization.lower().startswith("bearer "):
        raise _Unauthorized()
    return authorization.split(" ", 1)[1].strip()


def create_app() -> FastAPI:
    app = FastAPI(
        title="Neuron Companion API",
        version="0.1.0",
        description="Stateless AI companion runtime for Nebula CRM (F0038).",
    )

    @app.on_event("startup")
    async def _startup() -> None:
        # Fail-fast: an invalid orchestration asset raises here and the app won't serve.
        app.state.runtime = build_runtime()

    @app.exception_handler(NeuronError)
    async def _neuron_error_handler(request: Request, exc: NeuronError) -> JSONResponse:
        return _problem(
            status=exc.status,
            title=exc.title,
            detail=exc.detail,
            type_slug=type(exc).__name__,
            instance=str(request.url.path),
        )

    def runtime() -> NeuronRuntime:
        return app.state.runtime

    # --- Health ------------------------------------------------------------

    @app.get("/health", tags=["Health"])
    async def health() -> JSONResponse:
        rt: NeuronRuntime = runtime()
        return JSONResponse(status_code=200, content=rt.health_snapshot())

    @app.get("/ready", tags=["Health"])
    async def ready() -> JSONResponse:
        rt: NeuronRuntime = runtime()
        ok, detail = rt.readiness()
        return JSONResponse(status_code=200 if ok else 503, content={"ready": ok, **detail})

    # --- Companion (v1) ----------------------------------------------------

    @app.get("/v1/glance", tags=["Companion"])
    async def glance(request: Request, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        owner = subject_from_token(token)
        thread_id = request.query_params.get("thread_id")
        result = await GlanceAssembler(rt).assemble(
            user_token=token, owner_user_id=owner, thread_id=thread_id
        )
        return JSONResponse(status_code=200, content=result)

    @app.post("/v1/messages", tags=["Companion"])
    async def messages(request: Request, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        body = await request.json()
        owner = subject_from_token(token)
        envelope = await MessageDispatcher(rt).dispatch(
            text=body.get("text") or body.get("message"),
            thread_id=body.get("thread_id"),
            user_token=token,
            owner_user_id=owner,
        )
        return JSONResponse(status_code=200, content=envelope)

    @app.post("/v1/actions", tags=["Companion"])
    async def actions(request: Request, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        body = await request.json()
        action_type = body.get("action_type")
        if not action_type:
            return _problem(400, "Bad request", "action_type is required", "BadRequest")
        owner = subject_from_token(token)
        envelope = await ActionDispatcher(rt).dispatch(
            action_type=action_type,
            action_id=body.get("action_id"),
            payload=body.get("payload"),
            thread_id=body.get("thread_id"),
            user_token=token,
            owner_user_id=owner,
        )
        return JSONResponse(status_code=200, content=envelope)

    return app


app = create_app()
