from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

import structlog
from fastapi import FastAPI, HTTPException, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from src.api.middleware import RequestIdMiddleware
from src.api.routers.broker_insights import router as broker_insights_router
from src.api.routers.health import router as health_router
from src.infrastructure.db import Settings, close_engine, init_engine


def setup_logging(settings: Settings) -> None:
    structlog.configure(
        processors=[
            structlog.contextvars.merge_contextvars,
            structlog.processors.add_log_level,
            structlog.processors.TimeStamper(fmt="iso", utc=True),
            structlog.processors.JSONRenderer(),
        ],
        wrapper_class=structlog.make_filtering_bound_logger(settings.log_level_value),
        cache_logger_on_first_use=True,
    )


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    settings = app.state.settings
    setup_logging(settings)
    app.state.db_engine = init_engine(settings)
    structlog.get_logger().info("neuron_started", service_name=settings.service_name)
    try:
        yield
    finally:
        await close_engine()
        structlog.get_logger().info("neuron_stopped", service_name=settings.service_name)


def create_app(settings: Settings | None = None) -> FastAPI:
    resolved_settings = settings or Settings()
    app = FastAPI(
        title="Nebula Neuron — Python Data Layer",
        lifespan=lifespan,
    )
    app.state.settings = resolved_settings
    app.add_middleware(RequestIdMiddleware)
    app.include_router(health_router)
    app.include_router(broker_insights_router)
    _register_exception_handlers(app)
    return app


def _register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(HTTPException)
    async def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
        return _problem_response(
            request=request,
            status_code=exc.status_code,
            title="HTTP request failed",
            detail=str(exc.detail),
        )

    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(
        request: Request,
        exc: RequestValidationError,
    ) -> JSONResponse:
        return _problem_response(
            request=request,
            status_code=422,
            title="Request validation failed",
            detail="The request payload or parameters failed validation.",
            extra={"errors": exc.errors()},
        )

    @app.exception_handler(Exception)
    async def unhandled_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        structlog.get_logger().exception(
            "unhandled_exception",
            request_id=getattr(request.state, "request_id", None),
            error=str(exc),
        )
        return _problem_response(
            request=request,
            status_code=500,
            title="Internal server error",
            detail="An unexpected error occurred.",
        )


def _problem_response(
    request: Request,
    status_code: int,
    title: str,
    detail: str,
    extra: dict[str, object] | None = None,
) -> JSONResponse:
    body: dict[str, object] = {
        "type": "about:blank",
        "title": title,
        "status": status_code,
        "detail": detail,
        "instance": str(request.url.path),
        "requestId": getattr(request.state, "request_id", None),
    }
    if extra:
        body.update(extra)
    return JSONResponse(
        status_code=status_code,
        content=body,
        media_type="application/problem+json",
    )


app = create_app()
