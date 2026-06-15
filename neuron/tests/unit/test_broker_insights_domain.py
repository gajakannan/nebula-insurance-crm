from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from decimal import Decimal
from uuid import UUID, uuid4

from src.domain.broker_insights import (
    build_leaderboard,
    compute_scorecard,
    compute_trends,
)


@dataclass
class SubmissionFixture:
    id: UUID
    current_status: str
    premium_estimate: Decimal | None
    created_at: datetime
    line_of_business: str | None = None


@dataclass
class RenewalFixture:
    id: UUID
    current_status: str
    created_at: datetime
    line_of_business: str | None = None


@dataclass
class BrokerSummaryFixture:
    id: UUID
    legal_name: str
    state: str
    status: str
    submission_count: int
    renewal_count: int
    total_premium: Decimal | None


def test_compute_scorecard_empty_submissions_and_renewals_has_zero_rates() -> None:
    scorecard = compute_scorecard(uuid4(), "Test Broker", 90, [], [], 0)

    assert scorecard.quote_rate == 0.0
    assert scorecard.bind_rate == 0.0
    assert scorecard.retention_rate == 0.0


def test_compute_scorecard_quote_and_bind_rates() -> None:
    submissions = _submissions(
        ["Quoted", "Quoted", "BindRequested", "Bound", "Bound", "Bound", "Declined", "Declined", "New", "New"],
    )

    scorecard = compute_scorecard(uuid4(), "Test Broker", 90, submissions, [], 2)

    assert scorecard.total_submissions == 10
    assert scorecard.quoted_submissions == 6
    assert scorecard.bound_submissions == 3
    assert scorecard.declined_submissions == 2
    assert scorecard.quote_rate == 0.6
    assert scorecard.bind_rate == 0.3


def test_compute_scorecard_retention_rate() -> None:
    renewals = _renewals(["Completed", "Completed", "Completed", "Lost", "Lost"])

    scorecard = compute_scorecard(uuid4(), "Test Broker", 90, [], renewals, 0)

    assert scorecard.total_renewals == 5
    assert scorecard.completed_renewals == 3
    assert scorecard.lost_renewals == 2
    assert scorecard.retention_rate == 0.6


def test_compute_scorecard_all_bound_has_full_bind_rate() -> None:
    submissions = _submissions(["Bound", "Bound", "Bound", "Bound", "Bound"])

    scorecard = compute_scorecard(uuid4(), "Test Broker", 90, submissions, [], 0)

    assert scorecard.bind_rate == 1.0


def test_compute_trends_uses_week_granularity_for_90_day_window() -> None:
    trends = compute_trends(uuid4(), 90, _submissions(["Bound"]), [])

    assert trends.granularity == "week"


def test_compute_trends_uses_month_granularity_for_180_day_window() -> None:
    trends = compute_trends(uuid4(), 180, _submissions(["Bound"]), [])

    assert trends.granularity == "month"


def test_compute_trends_returns_points_sorted_by_period_label() -> None:
    broker_id = uuid4()
    submissions = [
        SubmissionFixture(
            id=uuid4(),
            current_status="Bound",
            premium_estimate=Decimal("250"),
            created_at=datetime(2026, 4, 15, tzinfo=timezone.utc),
        ),
        SubmissionFixture(
            id=uuid4(),
            current_status="Quoted",
            premium_estimate=Decimal("100"),
            created_at=datetime(2026, 1, 15, tzinfo=timezone.utc),
        ),
    ]

    trends = compute_trends(broker_id, 180, submissions, [])

    assert [point.period_label for point in trends.points] == ["2026-01", "2026-04"]


def test_build_leaderboard_assigns_rank_one_to_first_entry() -> None:
    leaderboard = build_leaderboard(_broker_summaries(1), 10)

    assert leaderboard[0].rank == 1


def test_build_leaderboard_respects_limit() -> None:
    leaderboard = build_leaderboard(_broker_summaries(5), 3)

    assert len(leaderboard) == 3


def test_build_leaderboard_converts_none_total_premium_to_zero() -> None:
    summaries = [
        BrokerSummaryFixture(
            id=uuid4(),
            legal_name="Null Premium Broker",
            state="CA",
            status="Active",
            submission_count=1,
            renewal_count=0,
            total_premium=None,
        ),
    ]

    leaderboard = build_leaderboard(summaries, 10)

    assert leaderboard[0].total_premium == Decimal("0")


def _submissions(statuses: list[str]) -> list[SubmissionFixture]:
    return [
        SubmissionFixture(
            id=uuid4(),
            current_status=status,
            premium_estimate=Decimal("100"),
            created_at=datetime(2026, 1, index + 1, tzinfo=timezone.utc),
        )
        for index, status in enumerate(statuses)
    ]


def _renewals(statuses: list[str]) -> list[RenewalFixture]:
    return [
        RenewalFixture(
            id=uuid4(),
            current_status=status,
            created_at=datetime(2026, 1, index + 1, tzinfo=timezone.utc),
        )
        for index, status in enumerate(statuses)
    ]


def _broker_summaries(count: int) -> list[BrokerSummaryFixture]:
    return [
        BrokerSummaryFixture(
            id=uuid4(),
            legal_name=f"Broker {index}",
            state="CA",
            status="Active",
            submission_count=index,
            renewal_count=index,
            total_premium=Decimal(str(1000 - index)),
        )
        for index in range(count)
    ]
