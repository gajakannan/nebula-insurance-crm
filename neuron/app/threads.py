"""Owner-scoped thread & history service (F0039-S0002).

Implements the ``/v1/threads*`` surface declared in ``planning-mds/api/neuron-api.yaml``
— that contract was authored at plan time and is the source of truth here; this module
implements it, it does not extend it.

Two invariants run through everything below:

* **Owner-scoping fails closed.** Every read and write is scoped by ``owner_user_id``
  in the repository. A non-owner gets exactly what a caller asking for a non-existent
  thread gets — the same 404 ``ProblemDetails``, with nothing revealed about whether
  the thread exists or what it contains.
* **The anchor is immutable.** There is no re-anchoring operation. Rename touches the
  title and nothing else; ``anchor_type``/``anchor_ref`` are fixed at creation.

Responses are the API's ``NeuronThread`` shape and versioned message envelopes — never
persistence row shapes, and never ``owner_user_id`` (it is server-derived).
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from . import envelope as env
from .errors import NeuronError, ThreadNotVisibleError
from .persistence.in_memory import validate_title
from .persistence.models import Message, Thread

if TYPE_CHECKING:
    from .runtime import NeuronRuntime

# Page bounds from neuron-api.yaml (listThreads / getThreadHistory).
LIST_DEFAULT_LIMIT = 20
LIST_MAX_LIMIT = 100
HISTORY_DEFAULT_LIMIT = 50
HISTORY_MAX_LIMIT = 100

MAX_ANCHOR_REF_LENGTH = 200
VALID_ANCHOR_TYPES = ("free_form", "domain", "record")

DEFAULT_FREE_FORM_TITLE = "New conversation"
# Deterministic domain labels — a title is never worth a model call (neuron-api.yaml:
# "no Phi call is made solely for auto-titling").
_DOMAIN_TITLES = {
    "day-at-a-glance": "Day at a Glance",
    "renewals": "Renewals",
    "tasks": "Tasks",
    "pipeline": "Pipeline",
    "broker-activity": "Broker Activity",
}


class InvalidAnchorError(NeuronError):
    """The anchor is missing, unknown, or inconsistent with its anchor_type."""

    status = 400
    title = "Invalid thread anchor"


def initial_title(anchor_type: str, anchor_ref: str | None, supplied: str | None) -> str:
    """Deterministic initial title (neuron-api.yaml createThread).

    A supplied title wins. Otherwise a domain/record anchor gets a stable label derived
    from its ref, and a free-form thread gets a fixed default. Purely deterministic —
    the same inputs always produce the same title, and no model is consulted.
    """
    if supplied is not None:
        return validate_title(supplied)
    if anchor_type == "domain" and anchor_ref:
        return _DOMAIN_TITLES.get(anchor_ref, anchor_ref.replace("-", " ").title())[
            :120
        ]
    if anchor_type == "record" and anchor_ref:
        return f"Record {anchor_ref}"[:120]
    return DEFAULT_FREE_FORM_TITLE


def validate_anchor(anchor_type: str, anchor_ref: str | None) -> tuple[str, str | None]:
    """Validate the immutable anchor pair at creation time."""
    if anchor_type not in VALID_ANCHOR_TYPES:
        raise InvalidAnchorError(
            f"anchor_type must be one of {', '.join(VALID_ANCHOR_TYPES)}"
        )
    if anchor_type == "free_form":
        # A free-form thread has no ref; accepting one would imply an anchor it does
        # not actually have.
        return anchor_type, None
    if not anchor_ref:
        raise InvalidAnchorError(f"anchor_ref is required for a {anchor_type} anchor")
    if len(anchor_ref) > MAX_ANCHOR_REF_LENGTH:
        raise InvalidAnchorError(
            f"anchor_ref must be at most {MAX_ANCHOR_REF_LENGTH} characters"
        )
    return anchor_type, anchor_ref


def clamp(value: Any, default: int, maximum: int) -> int:
    """Bound a client-supplied page size — an unbounded page is a DoS vector."""
    try:
        parsed = int(value)
    except (TypeError, ValueError):
        return default
    return max(1, min(parsed, maximum))


def thread_to_api(thread: Thread) -> dict[str, Any]:
    """Map a stored thread to the API's ``NeuronThread`` shape.

    ``owner_user_id`` is deliberately absent: it is server-derived and exposing it
    would leak the identity model to the client for no benefit.
    """
    return {
        "thread_id": thread.id,
        "anchor_type": thread.anchor_type,
        "anchor_ref": thread.anchor_ref,
        "title": thread.title or DEFAULT_FREE_FORM_TITLE,
        "created_at": thread.created_at.isoformat(),
        "updated_at": thread.updated_at.isoformat(),
        "last_sequence": thread.last_sequence,
    }


def message_to_envelope(message: Message) -> dict[str, Any]:
    """Replay a stored message as a versioned envelope (not a persistence row)."""
    parts = [part.content_json for part in sorted(message.parts, key=lambda p: p.ordinal)]
    return env.build_envelope(
        message.thread_id,
        role=message.role,
        parts=parts,
        message_id=message.id,
        in_reply_to_message_id=message.in_reply_to_message_id,
        created_at=message.created_at.isoformat(),
    )


class ThreadService:
    """The ``/v1/threads*`` operations, owner-scoped end to end."""

    def __init__(self, runtime: "NeuronRuntime") -> None:
        self._rt = runtime

    @property
    def _repo(self):
        return self._rt.repository

    # --- create / list / get -------------------------------------------------

    async def create(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
        thread_idempotency_key: str | None = None,
    ) -> dict[str, Any]:
        anchor_type, anchor_ref = validate_anchor(anchor_type, anchor_ref)
        thread = await self._repo.create_thread(
            owner_user_id,
            anchor_type=anchor_type,
            anchor_ref=anchor_ref,
            title=initial_title(anchor_type, anchor_ref, title),
            idempotency_key=thread_idempotency_key,
        )
        return thread_to_api(thread)

    async def list(
        self,
        owner_user_id: str,
        *,
        limit: Any = None,
        cursor: str | None = None,
    ) -> dict[str, Any]:
        page = await self._repo.list_threads(
            owner_user_id,
            limit=clamp(limit, LIST_DEFAULT_LIMIT, LIST_MAX_LIMIT),
            cursor=cursor,
        )
        return {
            "data": [thread_to_api(t) for t in page.items],
            "next_cursor": page.next_cursor,
        }

    async def get(self, thread_id: str, owner_user_id: str) -> dict[str, Any]:
        thread = await self._require_visible(thread_id, owner_user_id)
        return thread_to_api(thread)

    # --- mutate --------------------------------------------------------------

    async def rename(self, thread_id: str, owner_user_id: str, title: str) -> dict[str, Any]:
        # Title only — the anchor is immutable and there is no re-anchoring path.
        thread = await self._repo.rename_thread(thread_id, owner_user_id, title)
        return thread_to_api(thread)

    async def delete(self, thread_id: str, owner_user_id: str) -> None:
        await self._repo.delete_thread(thread_id, owner_user_id)

    # --- history -------------------------------------------------------------

    async def history(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        limit: Any = None,
        after: Any = None,
    ) -> dict[str, Any]:
        """Replay history in server-sequence order, cursor-paginated and resumable."""
        await self._require_visible(thread_id, owner_user_id)
        size = clamp(limit, HISTORY_DEFAULT_LIMIT, HISTORY_MAX_LIMIT)
        after_sequence = None
        if after is not None:
            try:
                after_sequence = max(0, int(after))
            except (TypeError, ValueError):
                after_sequence = None
        messages = await self._repo.get_messages(
            thread_id, owner_user_id, limit=size, after_sequence=after_sequence
        )
        # next_after is null when this page exhausted the thread, so the client stops
        # instead of polling for an empty page.
        next_after = messages[-1].sequence if len(messages) == size and messages else None
        return {
            "data": [message_to_envelope(m) for m in messages],
            "next_after": next_after,
        }

    # --- internals -----------------------------------------------------------

    async def _require_visible(self, thread_id: str, owner_user_id: str) -> Thread:
        thread = await self._repo.get_thread(thread_id, owner_user_id)
        if thread is None:
            # Identical to a genuine 404 — a non-owner learns nothing.
            raise ThreadNotVisibleError(f"thread {thread_id} not found")
        return thread
