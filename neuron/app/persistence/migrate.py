"""Apply the ``neuron.*`` forward migrations (F0039-S0001).

Neuron owns its schema (ADR-028 §1), so it owns applying it. Migrations are plain
``.sql`` files applied in filename order and recorded in ``neuron.schema_migrations``,
so re-running is a no-op — the operational property that matters when a container
restarts or a deploy replays.

Usage::

    python -m app.persistence.migrate --dsn "postgresql://user:pw@host:5432/nebula"

The DSN falls back to ``NEURON_POSTGRES_DSN``. It is never echoed.
"""

from __future__ import annotations

import argparse
import asyncio
import os
import sys
from pathlib import Path

import psycopg

MIGRATIONS_DIR = Path(__file__).resolve().parent / "migrations"

_LEDGER_DDL = """
CREATE SCHEMA IF NOT EXISTS neuron;
CREATE TABLE IF NOT EXISTS neuron.schema_migrations (
    filename    TEXT PRIMARY KEY,
    applied_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
"""


def migration_files() -> list[Path]:
    """Every ``NNNN_*.sql`` in filename order — the order is the contract."""
    return sorted(MIGRATIONS_DIR.glob("*.sql"))


async def apply_migrations(dsn: str, *, verbose: bool = True) -> list[str]:
    """Apply pending migrations; return the filenames actually applied."""
    applied: list[str] = []
    async with await psycopg.AsyncConnection.connect(dsn) as conn:
        async with conn.cursor() as cur:
            await cur.execute(_LEDGER_DDL)
            await cur.execute("SELECT filename FROM neuron.schema_migrations")
            done = {row[0] for row in await cur.fetchall()}
        await conn.commit()

        for path in migration_files():
            if path.name in done:
                continue
            sql = path.read_text(encoding="utf-8")
            # Each migration is one transaction: it applies completely or not at all.
            async with conn.transaction():
                async with conn.cursor() as cur:
                    await cur.execute(sql)
                    await cur.execute(
                        "INSERT INTO neuron.schema_migrations (filename) VALUES (%s)",
                        (path.name,),
                    )
            applied.append(path.name)
            if verbose:
                print(f"applied {path.name}")
    if verbose and not applied:
        print("no pending migrations")
    return applied


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Apply neuron.* schema migrations.")
    parser.add_argument("--dsn", default=os.environ.get("NEURON_POSTGRES_DSN", ""))
    args = parser.parse_args(argv)
    if not args.dsn:
        print("ERROR: --dsn or NEURON_POSTGRES_DSN is required", file=sys.stderr)
        return 2
    asyncio.run(apply_migrations(args.dsn))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
