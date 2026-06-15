from __future__ import annotations

from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel


class BaseDto(BaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        extra="forbid",
        populate_by_name=True,
    )


class HealthLiveDto(BaseDto):
    status: str


class HealthReadyDto(BaseDto):
    database: str
