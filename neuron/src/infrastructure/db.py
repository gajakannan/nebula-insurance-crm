from __future__ import annotations

from collections.abc import AsyncIterator

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict
from sqlalchemy.ext.asyncio import (
    AsyncEngine,
    AsyncSession,
    async_sessionmaker,
    create_async_engine,
)


class Settings(BaseSettings):
    model_config = SettingsConfigDict(extra="ignore")

    database_url: str = Field(
        default="postgresql+asyncpg://postgres:postgres@localhost:5433/nebula",
        alias="DATABASE_URL",
    )
    service_name: str = Field(default="nebula-neuron", alias="SERVICE_NAME")
    log_level: str = Field(default="info", alias="LOG_LEVEL")
    db_pool_size: int = Field(default=5, alias="DB_POOL_SIZE")
    db_max_overflow: int = Field(default=5, alias="DB_MAX_OVERFLOW")

    @property
    def log_level_value(self) -> int:
        levels = {
            "critical": 50,
            "error": 40,
            "warning": 30,
            "info": 20,
            "debug": 10,
            "notset": 0,
        }
        return levels.get(self.log_level.lower(), 20)


_engine: AsyncEngine | None = None
_session_factory: async_sessionmaker[AsyncSession] | None = None


def init_engine(settings: Settings) -> AsyncEngine:
    global _engine, _session_factory

    _engine = create_async_engine(
        settings.database_url,
        pool_size=settings.db_pool_size,
        max_overflow=settings.db_max_overflow,
        pool_pre_ping=True,
        connect_args={
            # asyncpg uses server_settings for libpq-style
            # "options=-c default_transaction_read_only=on" behavior.
            "server_settings": {"default_transaction_read_only": "on"},
        },
    )
    _session_factory = async_sessionmaker(
        bind=_engine,
        class_=AsyncSession,
        expire_on_commit=False,
    )
    return _engine


async def close_engine() -> None:
    global _engine, _session_factory

    if _engine is not None:
        await _engine.dispose()
    _engine = None
    _session_factory = None


async def get_session() -> AsyncIterator[AsyncSession]:
    if _session_factory is None:
        init_engine(Settings())

    if _session_factory is None:
        raise RuntimeError("Database session factory was not initialized.")

    async with _session_factory() as session:
        yield session
