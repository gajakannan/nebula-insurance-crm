from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from decimal import Decimal
from typing import Protocol, cast
from uuid import UUID


class SubmissionLike(Protocol):
    id: UUID
    current_status: str
    premium_estimate: Decimal | None
    created_at: datetime
    line_of_business: str | None


class RenewalLike(Protocol):
    id: UUID
    current_status: str
    created_at: datetime
    line_of_business: str | None


class BrokerSummaryLike(Protocol):
    id: UUID
    legal_name: str
    state: str
    status: str
    submission_count: int
    renewal_count: int
    total_premium: Decimal | None


@dataclass(frozen=True)
class BrokerScorecard:
    broker_id: UUID
    legal_name: str
    window_days: int
    total_submissions: int
    quoted_submissions: int
    bound_submissions: int
    declined_submissions: int
    quote_rate: float
    bind_rate: float
    total_renewals: int
    completed_renewals: int
    lost_renewals: int
    retention_rate: float
    total_premium_estimate: Decimal
    activity_count: int
    computed_at: datetime


@dataclass(frozen=True)
class BrokerTrendPoint:
    period_label: str
    submissions: int
    bound: int
    renewals_completed: int
    premium: Decimal


@dataclass(frozen=True)
class BrokerTrends:
    broker_id: UUID
    window_days: int
    granularity: str
    points: list[BrokerTrendPoint]


@dataclass(frozen=True)
class LeaderboardEntry:
    rank: int
    broker_id: UUID
    legal_name: str
    state: str
    submission_count: int
    renewal_count: int
    total_premium: Decimal


def compute_scorecard(
    broker_id: UUID,
    legal_name: str,
    window_days: int,
    submissions: list[SubmissionLike],
    renewals: list[RenewalLike],
    activity_count: int,
) -> BrokerScorecard:
    total_submissions = len(submissions)
    quoted_submissions = sum(
        1 for row in submissions if row.current_status in ("Quoted", "BindRequested", "Bound")
    )
    bound_submissions = sum(1 for row in submissions if row.current_status == "Bound")
    declined_submissions = sum(1 for row in submissions if row.current_status == "Declined")
    total_renewals = len(renewals)
    completed_renewals = sum(1 for row in renewals if row.current_status == "Completed")
    lost_renewals = sum(1 for row in renewals if row.current_status == "Lost")
    renewal_denominator = completed_renewals + lost_renewals
    total_premium_estimate = sum(
        (row.premium_estimate or Decimal("0") for row in submissions),
        Decimal("0"),
    )

    return BrokerScorecard(
        broker_id=broker_id,
        legal_name=legal_name,
        window_days=window_days,
        total_submissions=total_submissions,
        quoted_submissions=quoted_submissions,
        bound_submissions=bound_submissions,
        declined_submissions=declined_submissions,
        quote_rate=quoted_submissions / total_submissions if total_submissions else 0.0,
        bind_rate=bound_submissions / total_submissions if total_submissions else 0.0,
        total_renewals=total_renewals,
        completed_renewals=completed_renewals,
        lost_renewals=lost_renewals,
        retention_rate=completed_renewals / renewal_denominator if renewal_denominator else 0.0,
        total_premium_estimate=total_premium_estimate,
        activity_count=activity_count,
        computed_at=datetime.now(timezone.utc),
    )


def compute_trends(
    broker_id: UUID,
    window_days: int,
    submissions: list[SubmissionLike],
    renewals: list[RenewalLike],
) -> BrokerTrends:
    granularity = "week" if window_days <= 90 else "month"
    submission_rows = [
        {
            "period_label": _period_label(row.created_at, granularity),
            "submissions": 1,
            "bound": 1 if row.current_status == "Bound" else 0,
            "renewals_completed": 0,
            "premium": row.premium_estimate or Decimal("0"),
        }
        for row in submissions
    ]
    renewal_rows = [
        {
            "period_label": _period_label(row.created_at, granularity),
            "submissions": 0,
            "bound": 0,
            "renewals_completed": 1 if row.current_status == "Completed" else 0,
            "premium": Decimal("0"),
        }
        for row in renewals
    ]
    rows = submission_rows + renewal_rows
    if not rows:
        return BrokerTrends(
            broker_id=broker_id,
            window_days=window_days,
            granularity=granularity,
            points=[],
        )

    try:
        import pandas as pd
    except ModuleNotFoundError:
        points = _compute_trend_points_without_pandas(rows)
        return BrokerTrends(
            broker_id=broker_id,
            window_days=window_days,
            granularity=granularity,
            points=points,
        )

    frame = pd.DataFrame(rows)
    grouped = frame.groupby("period_label", as_index=False).agg(
        {
            "submissions": "sum",
            "bound": "sum",
            "renewals_completed": "sum",
            "premium": "sum",
        }
    )
    grouped = grouped.sort_values("period_label")
    points = [
        BrokerTrendPoint(
            period_label=str(row.period_label),
            submissions=int(row.submissions),
            bound=int(row.bound),
            renewals_completed=int(row.renewals_completed),
            premium=Decimal(str(row.premium)),
        )
        for row in grouped.itertuples(index=False)
    ]
    return BrokerTrends(
        broker_id=broker_id,
        window_days=window_days,
        granularity=granularity,
        points=points,
    )


def build_leaderboard(summaries: list[BrokerSummaryLike], limit: int) -> list[LeaderboardEntry]:
    return [
        LeaderboardEntry(
            rank=index + 1,
            broker_id=row.id,
            legal_name=row.legal_name,
            state=row.state,
            submission_count=row.submission_count,
            renewal_count=row.renewal_count,
            total_premium=row.total_premium or Decimal("0"),
        )
        for index, row in enumerate(summaries[:limit])
    ]


def _period_label(value: datetime, granularity: str) -> str:
    if granularity == "week":
        year, week, _weekday = value.isocalendar()
        return f"{year}-W{week:02d}"
    return value.strftime("%Y-%m")


def _compute_trend_points_without_pandas(rows: list[dict[str, object]]) -> list[BrokerTrendPoint]:
    buckets: dict[str, dict[str, object]] = {}
    for row in rows:
        period_label = cast(str, row["period_label"])
        bucket = buckets.setdefault(
            period_label,
            {
                "submissions": 0,
                "bound": 0,
                "renewals_completed": 0,
                "premium": Decimal("0"),
            },
        )
        bucket["submissions"] = cast(int, bucket["submissions"]) + cast(int, row["submissions"])
        bucket["bound"] = cast(int, bucket["bound"]) + cast(int, row["bound"])
        bucket["renewals_completed"] = cast(int, bucket["renewals_completed"]) + cast(
            int,
            row["renewals_completed"],
        )
        bucket["premium"] = cast(Decimal, bucket["premium"]) + cast(Decimal, row["premium"])

    return [
        BrokerTrendPoint(
            period_label=period_label,
            submissions=cast(int, bucket["submissions"]),
            bound=cast(int, bucket["bound"]),
            renewals_completed=cast(int, bucket["renewals_completed"]),
            premium=cast(Decimal, bucket["premium"]),
        )
        for period_label, bucket in sorted(buckets.items())
    ]
