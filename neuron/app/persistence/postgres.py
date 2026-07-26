"""Postgres ``NeuronRepository`` — the durable operation store (F0039-S0001).

ADR-028 §1 is authoritative: **Neuron owns and writes ``neuron.*`` directly** (the
engine database, not through the engine API). This is the store the F0038 in-memory
implementation was designed to be swapped for — same interface, same invariants,
restart-durable.

Design notes that matter:

* **Async all the way down.** Every method awaits a bounded ``AsyncConnectionPool``
  connection. Neuron's request paths are async; a blocking driver here would stall the
  event loop, which S0001's non-functional expectation forbids.
* **The server owns ordering.** A message's ``sequence`` is allocated by
  ``UPDATE neuron.threads SET next_sequence = next_sequence + 1 ... RETURNING`` inside
  the append transaction. The row lock serializes concurrent appends to one thread, so
  sequences are gapless and unique without a separate counter table — and the same
  statement bumps ``updated_at`` transactionally.
* **Idempotency is a database invariant**, not an application convention: the scoped
  partial unique indexes from ``0002`` are what actually prevent duplicates. The code
  reads back the original row on conflict rather than trusting a prior SELECT.
* **Owner-scoping is enforced in the WHERE clause of every statement**, so a
  non-owner's read returns nothing and a non-owner's write matches no row. It is never
  a post-fetch check in Python.
* **No raw message text is ever logged**, including on failure paths (ADR-027/028).
"""

from __future__ import annotations

from typing import Any

from psycopg import AsyncConnection, errors as pg_errors
from psycopg.rows import dict_row
from psycopg.types.json import Jsonb
from psycopg_pool import AsyncConnectionPool

from ..errors import (
    InvalidThreadTitleError,
    NeuronError,
    PersistenceUnavailableError,
    ThreadNotVisibleError,
)
from .in_memory import validate_title
from .models import (
    AgentRun,
    Message,
    MessagePart,
    ProvenanceEvent,
    Thread,
    ToolCall,
)
from .repository import DEFAULT_PAGE_SIZE, NeuronRepository, Page, clamp_limit

_THREAD_COLUMNS = (
    "id, owner_user_id, anchor_type, anchor_ref, title, thread_idempotency_key, "
    "next_sequence - 1 AS last_sequence, created_at, updated_at, deleted_at"
)
_MESSAGE_COLUMNS = (
    "id, thread_id, role, envelope_version, in_reply_to_message_id, sequence, "
    "client_message_key, created_at, updated_at"
)


def _thread(row: dict[str, Any]) -> Thread:
    return Thread(
        owner_user_id=row["owner_user_id"],
        anchor_type=row["anchor_type"],
        anchor_ref=row["anchor_ref"],
        title=row["title"],
        thread_idempotency_key=row["thread_idempotency_key"],
        last_sequence=row["last_sequence"],
        id=str(row["id"]),
        created_at=row["created_at"],
        updated_at=row["updated_at"],
        deleted_at=row["deleted_at"],
    )


def _message(row: dict[str, Any], parts: list[MessagePart] | None = None) -> Message:
    message = Message(
        thread_id=str(row["thread_id"]),
        role=row["role"],
        envelope_version=row["envelope_version"],
        in_reply_to_message_id=(
            str(row["in_reply_to_message_id"]) if row["in_reply_to_message_id"] else None
        ),
        sequence=row["sequence"],
        client_message_key=row["client_message_key"],
        id=str(row["id"]),
        created_at=row["created_at"],
        updated_at=row["updated_at"],
    )
    message.parts = parts or []
    return message


class PostgresNeuronRepository(NeuronRepository):
    """Durable ``neuron.*`` store over a bounded async connection pool."""

    def __init__(self, dsn: str, *, min_size: int = 1, max_size: int = 10) -> None:
        self._dsn = dsn
        self._min_size = min_size
        self._max_size = max_size
        self._pool: AsyncConnectionPool | None = None

    # --- lifecycle ----------------------------------------------------------

    async def startup(self) -> None:
        """Open the bounded pool. Bounded on purpose — an unbounded pool turns a
        traffic spike into database exhaustion for every other service on the box."""
        if self._pool is not None:
            return
        pool = AsyncConnectionPool(
            self._dsn,
            min_size=self._min_size,
            max_size=self._max_size,
            open=False,
            kwargs={"row_factory": dict_row},
        )
        await pool.open(wait=True, timeout=10.0)
        self._pool = pool

    async def shutdown(self) -> None:
        if self._pool is not None:
            await self._pool.close()
            self._pool = None

    def _require_pool(self) -> AsyncConnectionPool:
        if self._pool is None:
            raise PersistenceUnavailableError("conversation store pool is not open")
        return self._pool

    # --- threads ------------------------------------------------------------

    async def create_thread(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
        idempotency_key: str | None = None,
    ) -> Thread:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn:
                async with conn.cursor() as cur:
                    try:
                        await cur.execute(
                            f"""
                            INSERT INTO neuron.threads
                                (id, owner_user_id, anchor_type, anchor_ref, title,
                                 thread_idempotency_key)
                            VALUES (gen_random_uuid(), %s, %s, %s, %s, %s)
                            RETURNING {_THREAD_COLUMNS}
                            """,
                            (owner_user_id, anchor_type, anchor_ref, title, idempotency_key),
                        )
                    except pg_errors.UniqueViolation:
                        # The partial unique index fired: this owner already has a
                        # thread for this key. Return the original — the retry is a
                        # no-op, not a second conversation.
                        await conn.rollback()
                        existing = await self._thread_by_idempotency_key(
                            conn, owner_user_id, idempotency_key
                        )
                        if existing is None:
                            raise
                        return existing
                    row = await cur.fetchone()
            return _thread(row)
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not create thread") from exc

    async def _thread_by_idempotency_key(
        self, conn: AsyncConnection, owner_user_id: str, key: str | None
    ) -> Thread | None:
        async with conn.cursor() as cur:
            await cur.execute(
                f"""
                SELECT {_THREAD_COLUMNS} FROM neuron.threads
                WHERE owner_user_id = %s AND thread_idempotency_key = %s
                  AND deleted_at IS NULL
                """,
                (owner_user_id, key),
            )
            row = await cur.fetchone()
        return _thread(row) if row else None

    async def get_thread(self, thread_id: str, owner_user_id: str) -> Thread | None:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    f"""
                    SELECT {_THREAD_COLUMNS} FROM neuron.threads
                    WHERE id = %s AND owner_user_id = %s AND deleted_at IS NULL
                    """,
                    (thread_id, owner_user_id),
                )
                row = await cur.fetchone()
        except pg_errors.InvalidTextRepresentation:
            # A malformed uuid is "not found", never a 500 — and never a probe signal.
            return None
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not read thread") from exc
        return _thread(row) if row else None

    async def list_threads(
        self,
        owner_user_id: str,
        *,
        limit: int = DEFAULT_PAGE_SIZE,
        cursor: str | None = None,
    ) -> Page:
        size = clamp_limit(limit)
        pool = self._require_pool()
        params: list[Any] = [owner_user_id]
        keyset = ""
        if cursor:
            # Keyset pagination on (updated_at, id) — stable under concurrent writes
            # in a way OFFSET is not.
            anchor = await self.get_thread(cursor, owner_user_id)
            if anchor is not None:
                keyset = "AND (updated_at, id) < (%s, %s)"
                params += [anchor.updated_at, anchor.id]
        params.append(size + 1)  # one extra row tells us whether another page exists
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    f"""
                    SELECT {_THREAD_COLUMNS} FROM neuron.threads
                    WHERE owner_user_id = %s AND deleted_at IS NULL {keyset}
                    ORDER BY updated_at DESC, id DESC
                    LIMIT %s
                    """,
                    params,
                )
                rows = await cur.fetchall()
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not list threads") from exc
        threads = [_thread(r) for r in rows]
        next_cursor = threads[size - 1].id if len(threads) > size else None
        return Page(items=threads[:size], next_cursor=next_cursor)

    async def rename_thread(self, thread_id: str, owner_user_id: str, title: str) -> Thread:
        cleaned = validate_title(title)
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    f"""
                    UPDATE neuron.threads
                    SET title = %s, updated_at = now()
                    WHERE id = %s AND owner_user_id = %s AND deleted_at IS NULL
                    RETURNING {_THREAD_COLUMNS}
                    """,
                    (cleaned, thread_id, owner_user_id),
                )
                row = await cur.fetchone()
        except pg_errors.InvalidTextRepresentation:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner") from None
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not rename thread") from exc
        if row is None:
            # Non-owner and non-existent are indistinguishable by design.
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")
        return _thread(row)

    async def delete_thread(self, thread_id: str, owner_user_id: str) -> None:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    """
                    UPDATE neuron.threads
                    SET deleted_at = now(), updated_at = now()
                    WHERE id = %s AND owner_user_id = %s AND deleted_at IS NULL
                    RETURNING id
                    """,
                    (thread_id, owner_user_id),
                )
                row = await cur.fetchone()
        except pg_errors.InvalidTextRepresentation:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner") from None
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not delete thread") from exc
        if row is None:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")

    # --- messages -----------------------------------------------------------

    async def add_message(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        role: str,
        parts: list[tuple[str, dict[str, Any]]],
        envelope_version: int = 1,
        in_reply_to_message_id: str | None = None,
        client_message_key: str | None = None,
    ) -> Message:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn:
                # One transaction: allocate the sequence, insert the message, insert
                # its parts. A failure anywhere leaves no partial turn behind.
                async with conn.transaction():
                    async with conn.cursor() as cur:
                        # Allocating from the thread row both serializes concurrent
                        # appends (row lock) and proves ownership in the same
                        # statement — a non-owner matches no row.
                        await cur.execute(
                            """
                            UPDATE neuron.threads
                            SET next_sequence = next_sequence + 1, updated_at = now()
                            WHERE id = %s AND owner_user_id = %s AND deleted_at IS NULL
                            RETURNING next_sequence - 1 AS sequence
                            """,
                            (thread_id, owner_user_id),
                        )
                        allocated = await cur.fetchone()
                        if allocated is None:
                            raise ThreadNotVisibleError(
                                f"thread {thread_id} not visible to owner"
                            )
                        sequence = allocated["sequence"]

                        try:
                            await cur.execute(
                                f"""
                                INSERT INTO neuron.messages
                                    (id, thread_id, role, envelope_version,
                                     in_reply_to_message_id, sequence, client_message_key)
                                VALUES (gen_random_uuid(), %s, %s, %s, %s, %s, %s)
                                RETURNING {_MESSAGE_COLUMNS}
                                """,
                                (
                                    thread_id,
                                    role,
                                    envelope_version,
                                    in_reply_to_message_id,
                                    sequence,
                                    client_message_key,
                                ),
                            )
                        except pg_errors.UniqueViolation:
                            # Duplicate client_message_key — the idempotency index did
                            # its job. Roll back this attempt and return the original.
                            raise _DuplicateAppend from None
                        message_row = await cur.fetchone()

                        for ordinal, (part_type, content_json) in enumerate(parts):
                            await cur.execute(
                                """
                                INSERT INTO neuron.message_parts
                                    (id, message_id, ordinal, part_type, content_json)
                                VALUES (gen_random_uuid(), %s, %s, %s, %s)
                                """,
                                (
                                    message_row["id"],
                                    ordinal,
                                    part_type,
                                    # Jsonb, not a JSON string — the column is jsonb and
                                    # a bare str would be sent as text and rejected.
                                    Jsonb(content_json),
                                ),
                            )
        except _DuplicateAppend:
            existing = await self._message_by_client_key(thread_id, client_message_key)
            if existing is None:  # pragma: no cover - only on a concurrent hard delete
                raise PersistenceUnavailableError("duplicate append could not be resolved")
            return existing
        except (ThreadNotVisibleError, NeuronError):
            raise
        except pg_errors.InvalidTextRepresentation:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner") from None
        except pg_errors.Error as exc:
            # Fail safe before routing (S0001 business rule 4): the caller must not
            # proceed to intent resolution on an unpersisted turn. No raw text here.
            raise PersistenceUnavailableError("could not persist message") from exc
        return await self._load_message(str(message_row["id"]))

    async def _message_by_client_key(
        self, thread_id: str, client_message_key: str | None
    ) -> Message | None:
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                f"""
                SELECT {_MESSAGE_COLUMNS} FROM neuron.messages
                WHERE thread_id = %s AND client_message_key = %s
                """,
                (thread_id, client_message_key),
            )
            row = await cur.fetchone()
        return await self._load_message(str(row["id"])) if row else None

    async def _load_message(self, message_id: str) -> Message:
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                f"SELECT {_MESSAGE_COLUMNS} FROM neuron.messages WHERE id = %s",
                (message_id,),
            )
            row = await cur.fetchone()
            await cur.execute(
                """
                SELECT id, message_id, ordinal, part_type, content_json, created_at
                FROM neuron.message_parts WHERE message_id = %s ORDER BY ordinal
                """,
                (message_id,),
            )
            part_rows = await cur.fetchall()
        parts = [
            MessagePart(
                message_id=str(p["message_id"]),
                ordinal=p["ordinal"],
                part_type=p["part_type"],
                content_json=p["content_json"],
                id=str(p["id"]),
                created_at=p["created_at"],
            )
            for p in part_rows
        ]
        return _message(row, parts)

    async def get_messages(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        limit: int | None = None,
        after_sequence: int | None = None,
    ) -> list[Message]:
        if await self.get_thread(thread_id, owner_user_id) is None:
            # A non-owner gets an empty history, never another user's messages.
            return []
        pool = self._require_pool()
        params: list[Any] = [thread_id]
        seq_filter = ""
        if after_sequence is not None:
            seq_filter = "AND sequence > %s"
            params.append(after_sequence)
        params.append(clamp_limit(limit))
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    f"""
                    SELECT {_MESSAGE_COLUMNS} FROM neuron.messages
                    WHERE thread_id = %s {seq_filter}
                    ORDER BY sequence
                    LIMIT %s
                    """,
                    params,
                )
                message_rows = await cur.fetchall()
                if not message_rows:
                    return []
                ids = [r["id"] for r in message_rows]
                await cur.execute(
                    """
                    SELECT id, message_id, ordinal, part_type, content_json, created_at
                    FROM neuron.message_parts
                    WHERE message_id = ANY(%s)
                    ORDER BY message_id, ordinal
                    """,
                    (ids,),
                )
                part_rows = await cur.fetchall()
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not read history") from exc

        by_message: dict[str, list[MessagePart]] = {}
        for p in part_rows:
            by_message.setdefault(str(p["message_id"]), []).append(
                MessagePart(
                    message_id=str(p["message_id"]),
                    ordinal=p["ordinal"],
                    part_type=p["part_type"],
                    content_json=p["content_json"],
                    id=str(p["id"]),
                    created_at=p["created_at"],
                )
            )
        return [_message(r, by_message.get(str(r["id"]), [])) for r in message_rows]

    # --- A2A task trace -----------------------------------------------------

    async def create_agent_run(self, run: AgentRun) -> AgentRun:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    """
                    INSERT INTO neuron.agent_runs
                        (id, thread_id, parent_run_id, plan_id, plan_version, card_id,
                         card_version, card_content_hash, state, engine_ref_type,
                         engine_ref_id, created_at, updated_at)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                    """,
                    (
                        run.id, run.thread_id, run.parent_run_id, run.plan_id,
                        run.plan_version, run.card_id, run.card_version,
                        run.card_content_hash, run.state, run.engine_ref_type,
                        run.engine_ref_id, run.created_at, run.updated_at,
                    ),
                )
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not record agent run") from exc
        return run

    async def get_agent_run(self, run_id: str) -> AgentRun | None:
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                """
                SELECT id, thread_id, parent_run_id, plan_id, plan_version, card_id,
                       card_version, card_content_hash, state, engine_ref_type,
                       engine_ref_id, created_at, updated_at
                FROM neuron.agent_runs WHERE id = %s
                """,
                (run_id,),
            )
            row = await cur.fetchone()
        if row is None:
            return None
        return AgentRun(
            thread_id=str(row["thread_id"]),
            plan_id=row["plan_id"],
            plan_version=row["plan_version"],
            card_id=row["card_id"],
            card_version=row["card_version"],
            card_content_hash=row["card_content_hash"],
            state=row["state"],
            parent_run_id=str(row["parent_run_id"]) if row["parent_run_id"] else None,
            engine_ref_type=row["engine_ref_type"],
            engine_ref_id=row["engine_ref_id"],
            id=str(row["id"]),
            created_at=row["created_at"],
            updated_at=row["updated_at"],
        )

    async def update_run_state(self, run_id: str, state: str) -> AgentRun:
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                "UPDATE neuron.agent_runs SET state = %s, updated_at = now() WHERE id = %s",
                (state, run_id),
            )
        run = await self.get_agent_run(run_id)
        if run is None:
            raise NeuronError(f"agent run {run_id} not found")
        return run

    async def attach_engine_ref(
        self, run_id: str, engine_ref_type: str, engine_ref_id: str
    ) -> AgentRun:
        run = await self.get_agent_run(run_id)
        if run is None:
            raise NeuronError(f"agent run {run_id} not found")
        if run.engine_ref_id is not None:
            # WHY: idempotency key is the run id (ADR-028 §2). A repeat with the same
            # engine id is a safe no-op on cross-store retry; a different id would mean
            # one run claiming two engine writes — a corruption guard.
            if (run.engine_ref_type, run.engine_ref_id) != (engine_ref_type, engine_ref_id):
                raise NeuronError(
                    f"run {run_id} already references "
                    f"{run.engine_ref_type}:{run.engine_ref_id}"
                )
            return run
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                """
                UPDATE neuron.agent_runs
                SET engine_ref_type = %s, engine_ref_id = %s, updated_at = now()
                WHERE id = %s AND engine_ref_id IS NULL
                """,
                (engine_ref_type, engine_ref_id, run_id),
            )
        updated = await self.get_agent_run(run_id)
        if updated is None:  # pragma: no cover - the row was just read above
            raise NeuronError(f"agent run {run_id} not found")
        return updated

    async def record_tool_call(self, call: ToolCall) -> ToolCall:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    """
                    INSERT INTO neuron.tool_calls
                        (id, agent_run_id, tool_name, request_digest, status, latency_ms,
                         created_at)
                    VALUES (%s, %s, %s, %s, %s, %s, %s)
                    """,
                    (
                        call.id, call.agent_run_id, call.tool_name, call.request_digest,
                        call.status, call.latency_ms, call.created_at,
                    ),
                )
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not record tool call") from exc
        return call

    async def record_provenance(self, event: ProvenanceEvent) -> ProvenanceEvent:
        pool = self._require_pool()
        try:
            async with pool.connection() as conn, conn.cursor() as cur:
                await cur.execute(
                    """
                    INSERT INTO neuron.provenance_events
                        (id, agent_run_id, model, prompt_id, prompt_version, content_hash,
                         trace_id, cost, latency_ms, created_at)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                    """,
                    (
                        event.id, event.agent_run_id, event.model, event.prompt_id,
                        event.prompt_version, event.content_hash, event.trace_id,
                        event.cost, event.latency_ms, event.created_at,
                    ),
                )
        except pg_errors.Error as exc:
            raise PersistenceUnavailableError("could not record provenance") from exc
        return event

    async def list_provenance(self, run_id: str) -> list[ProvenanceEvent]:
        pool = self._require_pool()
        async with pool.connection() as conn, conn.cursor() as cur:
            await cur.execute(
                """
                SELECT id, agent_run_id, model, prompt_id, prompt_version, content_hash,
                       trace_id, cost, latency_ms, created_at
                FROM neuron.provenance_events
                WHERE agent_run_id = %s ORDER BY created_at
                """,
                (run_id,),
            )
            rows = await cur.fetchall()
        return [
            ProvenanceEvent(
                agent_run_id=str(r["agent_run_id"]),
                model=r["model"],
                content_hash=r["content_hash"],
                prompt_id=r["prompt_id"],
                prompt_version=r["prompt_version"],
                trace_id=r["trace_id"],
                cost=float(r["cost"]) if r["cost"] is not None else None,
                latency_ms=r["latency_ms"],
                id=str(r["id"]),
                created_at=r["created_at"],
            )
            for r in rows
        ]


class _DuplicateAppend(Exception):
    """Internal signal: the append hit the client-message-key unique index."""


# `validate_title` and `InvalidThreadTitleError` are shared with the in-memory store so
# both backends reject the same titles — referenced here to keep the import honest.
_ = (validate_title, InvalidThreadTitleError)
