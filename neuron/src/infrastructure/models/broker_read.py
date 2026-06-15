from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from uuid import UUID

from sqlalchemy import Boolean, DateTime, Numeric, String
from sqlalchemy.dialects.postgresql import UUID as PgUuid
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column


class Base(DeclarativeBase):
    pass


class BrokerModel(Base):
    __tablename__ = "Brokers"
    __allow_unmapped__ = True
    __table_args__ = {"extend_existing": True}

    Id: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), primary_key=True)
    LegalName: Mapped[str] = mapped_column(String, nullable=False)
    State: Mapped[str] = mapped_column(String, nullable=False)
    Status: Mapped[str] = mapped_column(String, nullable=False)
    IsDeleted: Mapped[bool] = mapped_column(Boolean, nullable=False)


class SubmissionModel(Base):
    __tablename__ = "Submissions"
    __allow_unmapped__ = True
    __table_args__ = {"extend_existing": True}

    Id: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), primary_key=True)
    BrokerId: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), nullable=False)
    CurrentStatus: Mapped[str] = mapped_column(String, nullable=False)
    LineOfBusiness: Mapped[str | None] = mapped_column(String, nullable=True)
    PremiumEstimate: Mapped[Decimal | None] = mapped_column(Numeric, nullable=True)
    CreatedAt: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    IsDeleted: Mapped[bool] = mapped_column(Boolean, nullable=False)


class RenewalModel(Base):
    __tablename__ = "Renewals"
    __allow_unmapped__ = True
    __table_args__ = {"extend_existing": True}

    Id: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), primary_key=True)
    BrokerId: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), nullable=False)
    CurrentStatus: Mapped[str] = mapped_column(String, nullable=False)
    LineOfBusiness: Mapped[str | None] = mapped_column(String, nullable=True)
    CreatedAt: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    IsDeleted: Mapped[bool] = mapped_column(Boolean, nullable=False)


class ActivityTimelineEventModel(Base):
    __tablename__ = "ActivityTimelineEvents"
    __allow_unmapped__ = True
    __table_args__ = {"extend_existing": True}

    Id: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), primary_key=True)
    EntityId: Mapped[UUID] = mapped_column(PgUuid(as_uuid=True), nullable=False)
    EntityType: Mapped[str] = mapped_column(String, nullable=False)
    EventType: Mapped[str] = mapped_column(String, nullable=False)
    OccurredAt: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
