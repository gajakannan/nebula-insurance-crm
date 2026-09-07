#!/usr/bin/env python3
"""Run the CRM evidence concession guard through the project-check contract."""

from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECK_ID = "evidence-durability"
DEFAULT_BOUNDARY = date(2026, 9, 7)


def load_guard():
    path = ROOT / "scripts/validate-evidence-package.py"
    spec = importlib.util.spec_from_file_location("validate_evidence_package", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load evidence guard: {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def current_run_ids(root: Path) -> list[str]:
    runs_root = root / "planning-mds/operations/evidence/runs"
    if not runs_root.is_dir():
        return []
    return sorted(
        manifest.parent.name
        for manifest in runs_root.glob("*/evidence-manifest.json")
        if manifest.is_file()
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--product-root", type=Path, default=ROOT)
    # These are bound by the framework manifest. The evidence check is
    # repository-wide because a plan-review run has no closeout run ID.
    parser.add_argument("--plan-scope", choices=["feature", "feature-set", "project"], required=True)
    parser.add_argument("--target", required=True)
    parser.add_argument("--minimum-recorded-on", default=DEFAULT_BOUNDARY.isoformat())
    args = parser.parse_args(argv)

    root = args.product_root.resolve()
    try:
        boundary = date.fromisoformat(args.minimum_recorded_on)
        guard = load_guard()
        errors: list[tuple[str, str]] = []
        for run_id in current_run_ids(root):
            for error in guard.check_run(root, run_id, boundary):
                errors.append((run_id, error))
    except (OSError, RuntimeError, ValueError) as exc:
        findings = [{
            "rule_id": "CRM-EVIDENCE-INVOCATION",
            "message": str(exc),
            "path": "planning-mds/operations/evidence/README.md",
        }]
        print(json.dumps({"schema_version": 1, "check_id": CHECK_ID, "status": "fail", "findings": findings}))
        return 1

    findings = [{
        "rule_id": "CRM-EVIDENCE-DURABILITY",
        "message": message,
        "path": f"planning-mds/operations/evidence/runs/{run_id}/evidence-manifest.json",
    } for run_id, message in errors]
    result = {
        "schema_version": 1,
        "check_id": CHECK_ID,
        "status": "fail" if findings else "pass",
        "findings": findings,
    }
    print(json.dumps(result))
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
