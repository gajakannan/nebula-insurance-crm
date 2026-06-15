from __future__ import annotations

from dataclasses import asdict
from uuid import UUID

from fastapi import APIRouter, Depends, Query
from sqlalchemy.ext.asyncio import AsyncSession

from src.application.dtos.broker_insights_dtos import (
    BrokerScorecardResponseDto,
    BrokerTrendsResponseDto,
    LeaderboardEntryDto,
    LeaderboardResponseDto,
    TrendPointDto,
)
from src.application.use_cases.broker_insights_use_cases import (
    GetBrokerScorecardUseCase,
    GetBrokerTrendsUseCase,
    GetLeaderboardUseCase,
)
from src.infrastructure.db import get_session
from src.infrastructure.repositories.broker_insights_repository import BrokerInsightsRepository

router = APIRouter(prefix="/api/v1/broker-insights", tags=["Broker Insights"])


@router.get("/{broker_id}/scorecard", response_model=BrokerScorecardResponseDto)
async def get_scorecard(
    broker_id: UUID,
    window_days: int = Query(default=90, ge=30, le=365),
    session: AsyncSession = Depends(get_session),
) -> BrokerScorecardResponseDto:
    repo = BrokerInsightsRepository(session)
    use_case = GetBrokerScorecardUseCase(repo)
    scorecard = await use_case.execute(broker_id, window_days)
    return BrokerScorecardResponseDto(**asdict(scorecard))


@router.get("/{broker_id}/trends", response_model=BrokerTrendsResponseDto)
async def get_trends(
    broker_id: UUID,
    window_days: int = Query(default=90, ge=30, le=365),
    session: AsyncSession = Depends(get_session),
) -> BrokerTrendsResponseDto:
    repo = BrokerInsightsRepository(session)
    use_case = GetBrokerTrendsUseCase(repo)
    trends = await use_case.execute(broker_id, window_days)
    return BrokerTrendsResponseDto(
        broker_id=trends.broker_id,
        window_days=trends.window_days,
        granularity=trends.granularity,
        points=[TrendPointDto(**asdict(point)) for point in trends.points],
    )


@router.get("/leaderboard", response_model=LeaderboardResponseDto)
async def get_leaderboard(
    limit: int = Query(default=10, ge=1, le=50),
    session: AsyncSession = Depends(get_session),
) -> LeaderboardResponseDto:
    repo = BrokerInsightsRepository(session)
    use_case = GetLeaderboardUseCase(repo)
    results = await use_case.execute(limit)
    entries = [LeaderboardEntryDto(**asdict(entry)) for entry in results]
    return LeaderboardResponseDto(entries=entries, total_brokers=len(entries))
