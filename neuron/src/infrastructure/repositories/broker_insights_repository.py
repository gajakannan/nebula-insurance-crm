from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from decimal import Decimal
from time import perf_counter
from uuid import UUID

import structlog
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncSession


@dataclass
class SubmissionRow:
    id: UUID
    current_status: str
    premium_estimate: Decimal | None
    created_at: datetime
    line_of_business: str | None


@dataclass
class RenewalRow:
    id: UUID
    current_status: str
    created_at: datetime
    line_of_business: str | None


@dataclass
class BrokerSummaryRow:
    id: UUID
    legal_name: str
    state: str
    status: str
    submission_count: int
    renewal_count: int
    total_premium: Decimal | None


class BrokerInsightsRepository:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session
        self._logger = structlog.get_logger()

    async def get_broker_name(self, broker_id: UUID) -> str | None:
        start = perf_counter()
        result = await self._session.execute(
            text(
                """
                SELECT "LegalName" FROM "Brokers"
                WHERE "Id" = :broker_id AND "IsDeleted" = false
                """,
            ),
            {"broker_id": broker_id},
        )
        self._log_slow_query("get_broker_name", start)
        return result.scalar_one_or_none()

    async def get_broker_submissions(
        self,
        broker_id: UUID,
        since: datetime,
    ) -> list[SubmissionRow]:
        start = perf_counter()
        result = await self._session.execute(
            text(
                """
                SELECT "Id","CurrentStatus","PremiumEstimate","CreatedAt","LineOfBusiness"
                FROM "Submissions"
                WHERE "BrokerId"=:broker_id AND "IsDeleted"=false AND "CreatedAt">=:since
                """,
            ),
            {"broker_id": broker_id, "since": since},
        )
        self._log_slow_query("get_broker_submissions", start)
        return [
            SubmissionRow(
                id=row.Id,
                current_status=row.CurrentStatus,
                premium_estimate=row.PremiumEstimate,
                created_at=row.CreatedAt,
                line_of_business=row.LineOfBusiness,
            )
            for row in result.fetchall()
        ]

    async def get_broker_renewals(
        self,
        broker_id: UUID,
        since: datetime,
    ) -> list[RenewalRow]:
        start = perf_counter()
        result = await self._session.execute(
            text(
                """
                SELECT "Id","CurrentStatus","CreatedAt","LineOfBusiness"
                FROM "Renewals"
                WHERE "BrokerId"=:broker_id AND "IsDeleted"=false AND "CreatedAt">=:since
                """,
            ),
            {"broker_id": broker_id, "since": since},
        )
        self._log_slow_query("get_broker_renewals", start)
        return [
            RenewalRow(
                id=row.Id,
                current_status=row.CurrentStatus,
                created_at=row.CreatedAt,
                line_of_business=row.LineOfBusiness,
            )
            for row in result.fetchall()
        ]

    async def get_broker_activity_count(self, broker_id: UUID, since: datetime) -> int:
        start = perf_counter()
        result = await self._session.execute(
            text(
                """
                SELECT COUNT(*) FROM "ActivityTimelineEvents"
                WHERE "EntityId"=:broker_id AND "OccurredAt">=:since
                """,
            ),
            {"broker_id": broker_id, "since": since},
        )
        self._log_slow_query("get_broker_activity_count", start)
        return int(result.scalar_one())

    async def get_all_brokers_summary(self) -> list[BrokerSummaryRow]:
        start = perf_counter()
        result = await self._session.execute(
            text(
                """
                SELECT b."Id", b."LegalName", b."State", b."Status",
                       COUNT(DISTINCT s."Id") AS submission_count,
                       COUNT(DISTINCT r."Id") AS renewal_count,
                       SUM(s."PremiumEstimate") AS total_premium
                FROM "Brokers" b
                LEFT JOIN "Submissions" s ON s."BrokerId"=b."Id" AND s."IsDeleted"=false
                LEFT JOIN "Renewals" r ON r."BrokerId"=b."Id" AND r."IsDeleted"=false
                WHERE b."IsDeleted"=false AND b."Status"='Active'
                GROUP BY b."Id", b."LegalName", b."State", b."Status"
                ORDER BY total_premium DESC NULLS LAST
                """,
            ),
        )
        self._log_slow_query("get_all_brokers_summary", start)
        return [
            BrokerSummaryRow(
                id=row.Id,
                legal_name=row.LegalName,
                state=row.State,
                status=row.Status,
                submission_count=int(row.submission_count),
                renewal_count=int(row.renewal_count),
                total_premium=row.total_premium,
            )
            for row in result.fetchall()
        ]

    def _log_slow_query(self, query_name: str, start: float) -> None:
        elapsed_ms = (perf_counter() - start) * 1000
        if elapsed_ms > 500:
            self._logger.warning(
                "broker_insights_slow_query",
                query_name=query_name,
                elapsed_ms=round(elapsed_ms, 2),
            )
