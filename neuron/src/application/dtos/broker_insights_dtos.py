from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from uuid import UUID

from src.application.dtos import BaseDto


class BrokerScorecardResponseDto(BaseDto):
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


class TrendPointDto(BaseDto):
    period_label: str
    submissions: int
    bound: int
    renewals_completed: int
    premium: Decimal


class BrokerTrendsResponseDto(BaseDto):
    broker_id: UUID
    window_days: int
    granularity: str
    points: list[TrendPointDto]


class LeaderboardEntryDto(BaseDto):
    rank: int
    broker_id: UUID
    legal_name: str
    state: str
    submission_count: int
    renewal_count: int
    total_premium: Decimal


class LeaderboardResponseDto(BaseDto):
    entries: list[LeaderboardEntryDto]
    total_brokers: int
