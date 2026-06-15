from __future__ import annotations

from collections.abc import Awaitable, Callable
from uuid import uuid4

import structlog
from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

REQUEST_ID_HEADER = "X-Request-ID"


class RequestIdMiddleware(BaseHTTPMiddleware):
    async def dispatch(
        self,
        request: Request,
        call_next: Callable[[Request], Awaitable[Response]],
    ) -> Response:
        request_id = request.headers.get(REQUEST_ID_HEADER) or str(uuid4())
        request.state.request_id = request_id

        bound_logger = structlog.get_logger().bind(
            request_id=request_id,
            method=request.method,
            path=request.url.path,
        )
        bound_logger.info("request_started")

        response = await call_next(request)
        response.headers[REQUEST_ID_HEADER] = request_id
        bound_logger.bind(status_code=response.status_code).info("request_completed")
        return response
