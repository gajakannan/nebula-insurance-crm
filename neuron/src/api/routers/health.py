from __future__ import annotations

import structlog
from fastapi import APIRouter, Depends, status
from fastapi.responses import JSONResponse
from sqlalchemy import text
from sqlalchemy.exc import SQLAlchemyError
from sqlalchemy.ext.asyncio import AsyncSession

from src.application.dtos import HealthLiveDto, HealthReadyDto
from src.infrastructure.db import get_session

router = APIRouter(prefix="/health", tags=["Health"])
logger = structlog.get_logger()


@router.get("/live", response_model=HealthLiveDto)
async def live() -> HealthLiveDto:
    return HealthLiveDto(status="alive")


@router.get("/ready", response_model=HealthReadyDto, responses={503: {"model": HealthReadyDto}})
async def ready(session: AsyncSession = Depends(get_session)) -> HealthReadyDto | JSONResponse:
    try:
        await session.execute(text("SELECT 1"))
    except SQLAlchemyError as exc:
        logger.warning("readiness_check_failed", error=str(exc))
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content=HealthReadyDto(database="unreachable").model_dump(),
        )

    return HealthReadyDto(database="ok")
