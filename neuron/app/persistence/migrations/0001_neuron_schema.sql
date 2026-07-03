-- Neuron operation store — durable home for the F0038 in-memory repository.
-- ADR-028 §1. Six tables, all UUID keys, audit columns (created_at/updated_at) by
-- convention. This schema holds ONLY companion operation state (threads, messages,
-- agent runs, tool calls, provenance) — never CRM/product business data, which stays
-- engine-owned. F0038 runs on the in-memory implementation; this migration scaffolds
-- the durable Postgres home the interface swaps onto (F0039+).
--
-- Provenance stores ids/versions/hashes/model/cost/latency/trace ONLY — never raw
-- prompts, raw LLM responses, or customer PII (ADR-027 security notes).

CREATE SCHEMA IF NOT EXISTS neuron;

-- Conversation thread; owner-scoped (private to creator). Maps to A2A contextId.
CREATE TABLE neuron.threads (
    id            UUID PRIMARY KEY,
    owner_user_id TEXT NOT NULL,
    anchor_type   TEXT NOT NULL DEFAULT 'free_form'
                       CHECK (anchor_type IN ('domain', 'record', 'free_form')),
    anchor_ref    TEXT,
    title         TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE INDEX ix_neuron_threads_owner ON neuron.threads (owner_user_id) WHERE deleted_at IS NULL;

-- One chat message, replayed via the versioned envelope.
CREATE TABLE neuron.messages (
    id                     UUID PRIMARY KEY,
    thread_id              UUID NOT NULL REFERENCES neuron.threads (id) ON DELETE CASCADE,
    role                   TEXT NOT NULL CHECK (role IN ('user', 'assistant')),
    in_reply_to_message_id UUID REFERENCES neuron.messages (id),
    envelope_version       INTEGER NOT NULL DEFAULT 1,
    created_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_neuron_messages_thread ON neuron.messages (thread_id, created_at);

-- Ordered envelope parts (neuron-message-envelope.schema.json).
CREATE TABLE neuron.message_parts (
    id           UUID PRIMARY KEY,
    message_id   UUID NOT NULL REFERENCES neuron.messages (id) ON DELETE CASCADE,
    ordinal      INTEGER NOT NULL,
    part_type    TEXT NOT NULL
                      CHECK (part_type IN ('text', 'app', 'status', 'sources', 'actions')),
    content_json JSONB NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (message_id, ordinal)
);

-- A2A task / subtask trace. References the active plan + Agent Card by id+version+hash
-- (definitions stay checked-in source assets). engine_ref_* references the authoritative
-- engine write for a cross-store gesture (ADR-028 §2).
CREATE TABLE neuron.agent_runs (
    id                 UUID PRIMARY KEY,
    thread_id          UUID NOT NULL REFERENCES neuron.threads (id) ON DELETE CASCADE,
    parent_run_id      UUID REFERENCES neuron.agent_runs (id),
    plan_id            TEXT NOT NULL,
    plan_version       TEXT NOT NULL,
    card_id            TEXT NOT NULL,
    card_version       TEXT NOT NULL,
    card_content_hash  TEXT NOT NULL,
    state              TEXT NOT NULL DEFAULT 'submitted'
                            CHECK (state IN ('submitted', 'working', 'input_required',
                                             'completed', 'failed', 'canceled')),
    engine_ref_type    TEXT,
    engine_ref_id      TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_neuron_agent_runs_thread ON neuron.agent_runs (thread_id);
-- One engine write per run: the idempotency key for the cross-store record (ADR-028 §2).
CREATE UNIQUE INDEX ux_neuron_agent_runs_engine_ref
    ON neuron.agent_runs (engine_ref_type, engine_ref_id)
    WHERE engine_ref_id IS NOT NULL;

-- MCP/tool invocation under a task. No raw args/PII — only a digest.
CREATE TABLE neuron.tool_calls (
    id             UUID PRIMARY KEY,
    agent_run_id   UUID NOT NULL REFERENCES neuron.agent_runs (id) ON DELETE CASCADE,
    tool_name      TEXT NOT NULL,
    request_digest TEXT NOT NULL,
    status         TEXT NOT NULL,
    latency_ms     INTEGER,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_neuron_tool_calls_run ON neuron.tool_calls (agent_run_id);

-- Draft/mock-send provenance and run metadata. NO raw prompts/responses/PII.
CREATE TABLE neuron.provenance_events (
    id             UUID PRIMARY KEY,
    agent_run_id   UUID NOT NULL REFERENCES neuron.agent_runs (id) ON DELETE CASCADE,
    model          TEXT NOT NULL,
    prompt_id      TEXT,
    prompt_version TEXT,
    content_hash   TEXT NOT NULL,
    trace_id       TEXT,
    cost           NUMERIC(12, 6),
    latency_ms     INTEGER,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_neuron_provenance_run ON neuron.provenance_events (agent_run_id);
