#!/usr/bin/env python3
"""Validate durability and contract identity for newly changed feature runs.

The framework validator remains the authority for gate semantics. This product
check catches the repository failures that caused R05 before closeout: missing
contract identity, scratch or secret paths, and artifact references that do not
resolve to durable files.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

RUN_PATH_RE = re.compile(r"^planning-mds/operations/evidence/runs/([^/]+)/")
DATE_RE = re.compile(r"^\d{4}-\d{2}-\d{2}$")
ABSOLUTE_RE = re.compile(r"^(?:/|[A-Za-z]:[\\/])")


def changed_files(base: str | None) -> list[str]:
    if not base:
        return []
    if set(base) == {"0"}:
        base = "HEAD^"
    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", f"{base}...HEAD"],
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        raise SystemExit(f"cannot compare evidence changes against {base}: {exc.stderr.strip()}")
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def run_ids_from_files(files: list[str]) -> list[str]:
    run_ids = set()
    for rel in files:
        match = RUN_PATH_RE.match(rel)
        if match:
            run_ids.add(match.group(1))
    return sorted(run_ids)


def resolve_artifact(run_dir: Path, product_root: Path, raw: str) -> Path | None:
    value = raw.strip()
    if not value or ABSOLUTE_RE.match(value) or value.startswith("~"):
        return None
    normalized = value.replace("\\", "/")
    if normalized == ".env" or normalized.endswith("/.env"):
        return None
    if normalized.startswith("/tmp/") or normalized.startswith("/home/"):
        return None
    if normalized.startswith("planning-mds/"):
        return product_root / normalized
    return run_dir / normalized


def check_run(product_root: Path, run_id: str, minimum_date: date) -> list[str]:
    run_dir = product_root / "planning-mds/operations/evidence/runs" / run_id
    manifest_path = run_dir / "evidence-manifest.json"
    if not manifest_path.is_file():
        return [f"{run_id}: missing evidence-manifest.json"]
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{run_id}: cannot read evidence-manifest.json: {exc}"]

    recorded_on = manifest.get("recorded_on")
    if not isinstance(recorded_on, str) or not DATE_RE.match(recorded_on):
        return [f"{run_id}: recorded_on must be ISO YYYY-MM-DD"]
    if date.fromisoformat(recorded_on) < minimum_date:
        return []  # Explicit historical compatibility boundary.

    errors: list[str] = []
    if manifest.get("run_id") != run_id:
        errors.append(f"{run_id}: manifest run_id does not match its folder")
    for key in ("contract_version", "contract_effective_date"):
        value = manifest.get(key)
        if not isinstance(value, str) or not DATE_RE.match(value):
            errors.append(f"{run_id}: manifest {key} must be an ISO contract pin")

    for name in ("action-context.md", "artifact-trace.md", "commands.log", "lifecycle-gates.log"):
        if not (run_dir / name).is_file():
            errors.append(f"{run_id}: missing durable run file {name}")

    commands_path = run_dir / "commands.log"
    if commands_path.is_file():
        for line_number, line in enumerate(commands_path.read_text(encoding="utf-8").splitlines(), 1):
            if not line.strip():
                continue
            try:
                event = json.loads(line)
            except json.JSONDecodeError as exc:
                errors.append(f"{run_id}: commands.log line {line_number} is not JSON: {exc.msg}")
                continue
            artifacts = event.get("artifacts", [])
            if not isinstance(artifacts, list):
                errors.append(f"{run_id}: commands.log line {line_number} artifacts must be a list")
                continue
            for artifact in artifacts:
                if not isinstance(artifact, str):
                    errors.append(f"{run_id}: commands.log line {line_number} has a non-string artifact")
                    continue
                path = resolve_artifact(run_dir, product_root, artifact)
                if path is None:
                    errors.append(f"{run_id}: commands.log line {line_number} has non-durable artifact {artifact!r}")
                elif not path.exists():
                    errors.append(f"{run_id}: commands.log line {line_number} references missing artifact {artifact!r}")

    files = manifest.get("files", {})
    if isinstance(files, dict):
        for name, value in files.items():
            if not isinstance(value, str) or not value:
                continue
            path = resolve_artifact(run_dir, product_root, value)
            if path is None or not path.exists():
                errors.append(f"{run_id}: manifest files.{name} does not resolve to durable evidence: {value!r}")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--product-root", type=Path, default=Path.cwd())
    parser.add_argument("--base", help="Git base ref; only changed evidence runs are checked")
    parser.add_argument("--run", action="append", dest="runs", help="Explicit run ID; may be repeated")
    parser.add_argument(
        "--minimum-recorded-on",
        default="2026-09-07",
        help="Historical compatibility boundary (default: 2026-09-07)",
    )
    args = parser.parse_args()
    product_root = args.product_root.resolve()
    try:
        minimum_date = date.fromisoformat(args.minimum_recorded_on)
    except ValueError as exc:
        parser.error(f"invalid --minimum-recorded-on: {exc}")

    files = changed_files(args.base) if args.base else []
    runs = sorted(set(args.runs or []) | set(run_ids_from_files(files)))
    if not runs:
        print("Evidence durability check: no changed feature runs.")
        return 0

    errors = []
    for run_id in runs:
        errors.extend(check_run(product_root, run_id, minimum_date))
    if errors:
        print("Evidence durability check: FAIL")
        print("\n".join(f"- {error}" for error in errors))
        return 1
    print(f"Evidence durability check: PASS ({len(runs)} run(s) checked).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
