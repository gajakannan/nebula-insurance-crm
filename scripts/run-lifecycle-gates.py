#!/usr/bin/env python3
"""Run product-local gates declared in lifecycle-stage.yaml."""

from __future__ import annotations

import argparse
import subprocess
from pathlib import Path

import yaml

REPO_ROOT = Path(__file__).resolve().parents[1]
CONFIG = REPO_ROOT / "lifecycle-stage.yaml"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stage", default="", help="Stage override; defaults to current_stage")
    parser.add_argument("--list", action="store_true", help="Print the stage/gate matrix and exit")
    args = parser.parse_args()

    config = yaml.safe_load(CONFIG.read_text(encoding="utf-8"))
    stages, gates = config["stages"], config["gates"]
    stage = args.stage or config["current_stage"]
    if stage not in stages:
        print(f"[ERROR] unknown lifecycle stage {stage!r}")
        return 2
    if args.list:
        for name, spec in stages.items():
            print(f"{name}: {spec.get('description', '').strip()}")
            for gate in spec.get("required_gates", []) or ["(none)"]:
                print(f"  - {gate}")
        print(f"Current stage: {config['current_stage']}")
        return 0

    failures: list[str] = []
    print(f"Running lifecycle gates for stage: {stage}")
    for name in stages[stage].get("required_gates", []):
        gate = gates.get(name)
        if not isinstance(gate, dict) or not isinstance(gate.get("command"), list):
            print(f"[ERROR] gate {name!r} has no valid command")
            failures.append(name)
            continue
        command = gate["command"]
        print(f"[GATE] {name}\n  {gate.get('description', '').strip()}\n  command: {' '.join(command)}")
        result = subprocess.run(command, cwd=REPO_ROOT)
        print(f"[{'PASS' if result.returncode == 0 else 'FAIL'}] {name}\n")
        if result.returncode != 0:
            failures.append(name)
    if failures:
        print(f"[SUMMARY] FAILED ({len(failures)}): {', '.join(failures)}")
        return 1
    print(f"[SUMMARY] PASSED ({len(stages[stage].get('required_gates', []))} gate(s))")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
