-- F0039-S0001 — server-owned message ordering + idempotent appends.
-- Forward migration over the 0001 scaffold. ADR-028 §1 (Neuron owns neuron.*).
-- The six 0001 tables are NOT recreated — this only adds columns and indexes.
--
-- WHY a server sequence: history replay and cursor pagination (S0002) must be
-- deterministic. created_at is not enough — two messages can share a timestamp, and
-- clients must never influence order. The sequence is assigned by the server inside
-- the same transaction as the insert.
--
-- WHY idempotency keys: a retried send (network retry, double-submit, Daily Brief
-- re-render) must not create a second row. The scoped partial unique indexes make
-- that a database invariant rather than an application convention.

-- --------------------------------------------------------------------------
-- Per-thread sequence allocator.
-- Held on the thread row so allocation is a single atomic UPDATE ... RETURNING
-- (row lock), which also gives us the transactional updated_at the story requires.
-- --------------------------------------------------------------------------
ALTER TABLE neuron.threads
    ADD COLUMN IF NOT EXISTS next_sequence BIGINT NOT NULL DEFAULT 1;

-- Thread-creation idempotency: a retried create for the same owner + key returns the
-- original thread instead of opening a duplicate conversation.
ALTER TABLE neuron.threads
    ADD COLUMN IF NOT EXISTS thread_idempotency_key TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS ux_neuron_threads_owner_idem
    ON neuron.threads (owner_user_id, thread_idempotency_key)
    WHERE thread_idempotency_key IS NOT NULL;

-- --------------------------------------------------------------------------
-- Server-assigned message ordering.
-- --------------------------------------------------------------------------
ALTER TABLE neuron.messages
    ADD COLUMN IF NOT EXISTS sequence BIGINT;

-- Backfill any pre-existing rows deterministically by (created_at, id) so the
-- NOT NULL + unique constraints below can be applied to an already-populated table.
WITH ordered AS (
    SELECT id,
           row_number() OVER (PARTITION BY thread_id ORDER BY created_at, id) AS rn
    FROM neuron.messages
    WHERE sequence IS NULL
)
UPDATE neuron.messages m
SET sequence = ordered.rn
FROM ordered
WHERE m.id = ordered.id;

-- Keep each thread's allocator ahead of any backfilled rows.
UPDATE neuron.threads t
SET next_sequence = sub.next_seq
FROM (
    SELECT thread_id, MAX(sequence) + 1 AS next_seq
    FROM neuron.messages
    GROUP BY thread_id
) AS sub
WHERE t.id = sub.thread_id
  AND t.next_sequence < sub.next_seq;

ALTER TABLE neuron.messages
    ALTER COLUMN sequence SET NOT NULL;

-- The ordering invariant: one sequence per position per thread.
CREATE UNIQUE INDEX IF NOT EXISTS ux_neuron_messages_thread_sequence
    ON neuron.messages (thread_id, sequence);

-- History replay and cursor pagination read along this index.
CREATE INDEX IF NOT EXISTS ix_neuron_messages_thread_seq
    ON neuron.messages (thread_id, sequence);

-- --------------------------------------------------------------------------
-- Append idempotency.
-- Covers both the client message key and the Daily Brief key — the Daily Brief
-- supplies a stable per-day key so re-rendering the brief never duplicates it.
-- --------------------------------------------------------------------------
ALTER TABLE neuron.messages
    ADD COLUMN IF NOT EXISTS client_message_key TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS ux_neuron_messages_thread_client_key
    ON neuron.messages (thread_id, client_message_key)
    WHERE client_message_key IS NOT NULL;
