from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import UUID

from src.domain.broker_insights import (
    BrokerScorecard,
    BrokerTrends,
    LeaderboardEntry,
    build_leaderboard,
    compute_scorecard,
    compute_trends,
)
from src.infrastructure.repositories.broker_insights_repository import BrokerInsightsRepository


class GetBrokerScorecardUseCase:
    def __init__(self, repo: BrokerInsightsRepository) -> None:
        self.repo = repo

    async def execute(self, broker_id: UUID, window_days: int = 90) -> BrokerScorecard:
        since = datetime.now(timezone.utc) - timedelta(days=window_days)
        legal_name = await self.repo.get_broker_name(broker_id) or "Unknown"
        submissions = await self.repo.get_broker_submissions(broker_id, since)
        renewals = await self.repo.get_broker_renewals(broker_id, since)
        activity = await self.repo.get_broker_activity_count(broker_id, since)
        return compute_scorecard(
            broker_id,
            legal_name,
            window_days,
            submissions,
            renewals,
            activity,
        )


class GetBrokerTrendsUseCase:
    def __init__(self, repo: BrokerInsightsRepository) -> None:
        self.repo = repo

    async def execute(self, broker_id: UUID, window_days: int = 90) -> BrokerTrends:
        since = datetime.now(timezone.utc) - timedelta(days=window_days)
        submissions = await self.repo.get_broker_submissions(broker_id, since)
        renewals = await self.repo.get_broker_renewals(broker_id, since)
        return compute_trends(broker_id, window_days, submissions, renewals)


class GetLeaderboardUseCase:
    def __init__(self, repo: BrokerInsightsRepository) -> None:
        self.repo = repo

    async def execute(self, limit: int = 10) -> list[LeaderboardEntry]:
        summaries = await self.repo.get_all_brokers_summary()
        return build_leaderboard(summaries, limit)
