"""FastAPI entrypoint for the Neuron Companion (neuron-api.yaml).

Thin by design: it builds the validated runtime at startup (fail-fast) and exposes
the HTTP surface. ``/health`` + ``/ready`` (S0001), ``GET /v1/glance`` (S0002 zone
assembly), ``POST /v1/actions`` (S0003/S0005/S0006 component callbacks), and
``POST /v1/messages`` (S0007 scope-guarded conversational send) are all live. All
business logic lives in the runtime modules, which are framework-agnostic and
unit-tested without FastAPI.
"""

from __future__ import annotations

from contextlib import asynccontextmanager

from fastapi import Depends, FastAPI, Header, Request, Response
from fastapi.responses import JSONResponse

from .auth import subject_from_token
from .actions import ActionDispatcher
from .bootstrap import build_runtime
from .errors import NeuronError
from .messages import MessageDispatcher
from .orchestration.glance import GlanceAssembler
from .runtime import NeuronRuntime
from .threads import ThreadService

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
    @asynccontextmanager
    async def lifespan(app: FastAPI):
        # Fail-fast: an invalid orchestration asset raises here and the app won't serve.
        app.state.runtime = build_runtime()
        # F0039-S0001: open the durable store's pool before serving. A store that
        # cannot open is a startup failure, not a per-request surprise.
        await app.state.runtime.repository.startup()
        try:
            yield
        finally:
            # Release pooled connections on the way down so a restart doesn't leave
            # sessions held open against the engine database.
            await app.state.runtime.repository.shutdown()

    app = FastAPI(
        title="Neuron Companion API",
        version="0.1.0",
        description="Stateless AI companion runtime for Nebula CRM (F0038/F0039).",
        lifespan=lifespan,
    )

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

    # --- Threads (v1, F0039-S0002) -----------------------------------------
    # Every handler derives the owner from the forwarded token — a thread id in the
    # path never selects whose data is returned.

    @app.post("/v1/threads", tags=["Threads"], status_code=201)
    async def create_thread(request: Request, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        body = await request.json()
        owner = subject_from_token(token)
        thread = await ThreadService(rt).create(
            owner,
            anchor_type=body.get("anchor_type", "free_form"),
            anchor_ref=body.get("anchor_ref"),
            title=body.get("title"),
            thread_idempotency_key=body.get("thread_idempotency_key"),
        )
        return JSONResponse(status_code=201, content=thread)

    @app.get("/v1/threads", tags=["Threads"])
    async def list_threads(request: Request, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        owner = subject_from_token(token)
        page = await ThreadService(rt).list(
            owner,
            limit=request.query_params.get("limit"),
            cursor=request.query_params.get("cursor"),
        )
        return JSONResponse(status_code=200, content=page)

    @app.get("/v1/threads/{thread_id}", tags=["Threads"])
    async def get_thread(thread_id: str, token: str = Depends(require_bearer)) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        owner = subject_from_token(token)
        return JSONResponse(status_code=200, content=await ThreadService(rt).get(thread_id, owner))

    @app.patch("/v1/threads/{thread_id}", tags=["Threads"])
    async def rename_thread(
        thread_id: str, request: Request, token: str = Depends(require_bearer)
    ) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        body = await request.json()
        if "title" not in body:
            return _problem(400, "Bad request", "title is required", "BadRequest")
        owner = subject_from_token(token)
        thread = await ThreadService(rt).rename(thread_id, owner, body["title"])
        return JSONResponse(status_code=200, content=thread)

    @app.delete("/v1/threads/{thread_id}", tags=["Threads"], status_code=204)
    async def delete_thread(thread_id: str, token: str = Depends(require_bearer)) -> Response:
        rt: NeuronRuntime = runtime()
        owner = subject_from_token(token)
        await ThreadService(rt).delete(thread_id, owner)
        return Response(status_code=204)

    @app.get("/v1/threads/{thread_id}/messages", tags=["Threads"])
    async def thread_history(
        thread_id: str, request: Request, token: str = Depends(require_bearer)
    ) -> JSONResponse:
        rt: NeuronRuntime = runtime()
        owner = subject_from_token(token)
        page = await ThreadService(rt).history(
            thread_id,
            owner,
            limit=request.query_params.get("limit"),
            after=request.query_params.get("after"),
        )
        return JSONResponse(status_code=200, content=page)

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
