"""Versioned evaluation command (F0039-S0008).

    python -m app.intent.evaluate_cli --report evals/reports/<name>.json

Runs the reviewed datasets through the real resolver against whatever provider is
configured, prints the §30.4 gate table, and writes a reproducible JSON report.

Exit code is **1 when any gate fails**, so this is usable directly as the rollout gate:
direct routing is enabled only on a green run (spec §33 Phase 3).
"""

from __future__ import annotations

import argparse
import asyncio
import json
import sys
from pathlib import Path

from ..bootstrap import build_runtime
from .evaluation import GATES, evaluate
from .resolver import build_resolver


def _format(report) -> str:
    lines = ["", "§30.4 acceptance gates", "-" * 72]
    for gate in GATES:
        value = report.metrics.get(gate.key, 0.0)
        passed = report.gate_results.get(gate.key, False)
        target = f"{'<=' if gate.is_ceiling else '>='} {gate.threshold:g}"
        shown = f"{value:.0f}" if gate.is_ceiling else f"{value:.3f}"
        lines.append(f"{'PASS' if passed else 'FAIL'}  {gate.key:24s} {shown:>7s}  (target {target})")
    lines.append("-" * 72)
    lines.append(f"cases: {report.counts}")
    if report.failed_case_ids:
        lines.append(f"failed case ids: {', '.join(report.failed_case_ids)}")
    lines.append(f"OVERALL: {'PASS' if report.all_gates_passed else 'FAIL'}")
    return "\n".join(lines)


async def _main(args) -> int:
    runtime = build_runtime()
    resolver = build_resolver(runtime)
    report = await evaluate(
        resolver,
        provenance_extra={
            "model_provider": runtime.model_router.default,
            "intent_mode": runtime.settings.intent_mode,
            "persistence_backend": runtime.settings.persistence_backend,
        },
    )
    print(_format(report))
    if args.report:
        path = Path(args.report)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(report.to_dict(), indent=2) + "\n", encoding="utf-8")
        print(f"\nreport written to {path}")
    return 0 if report.all_gates_passed else 1


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Evaluate the intent resolver (§30.4 gates).")
    parser.add_argument("--report", help="Write a JSON report to this path.")
    return asyncio.run(_main(parser.parse_args(argv)))


if __name__ == "__main__":  # pragma: no cover
    sys.exit(main())
