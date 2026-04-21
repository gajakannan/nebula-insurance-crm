#!/usr/bin/env python3
"""
Validate Nebula's solution-owned frontend quality evidence manifest.

This gate is intentionally solution-specific. It verifies that the required
frontend validation layers were executed and that the referenced
coverage/evidence artifacts exist on disk.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, Iterable, List


DEFAULT_MANIFEST = Path("planning-mds/operations/evidence/frontend-quality/latest-run.json")
REQUIRED_LAYERS = ("component", "integration", "accessibility", "coverage", "visual")
REQUIRED_TOP_LEVEL_ARTIFACTS = (
    "commands_log",
    "lifecycle_gates_log",
    "story_to_suite",
    "action_context",
    "artifact_trace",
    "gate_decisions",
)
GENERATED_COVERAGE_ARTIFACTS = (
    "experience/coverage/lcov.info",
    "experience/coverage/coverage-summary.json",
)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def read_manifest(path: Path) -> Dict:
    if not path.exists():
        raise ValueError(f"Frontend quality manifest not found: {path}")

    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    if not isinstance(data, dict):
        raise ValueError("Frontend quality manifest must be a JSON object")

    return data


def normalize_repo_path(path_value: str, root: Path) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else root / path


def require_string(data: Dict, key: str, errors: List[str]) -> str:
    value = data.get(key)
    if not isinstance(value, str) or not value.strip():
        errors.append(f"Missing required string field: {key}")
        return ""
    return value


def require_existing_file(root: Path, label: str, path_value: str, errors: List[str]) -> None:
    path = normalize_repo_path(path_value, root)
    if not path.is_file():
        errors.append(f"{label} artifact missing: {path_value}")


def require_existing_directory(root: Path, label: str, path_value: str, errors: List[str]) -> None:
    path = normalize_repo_path(path_value, root)
    if not path.is_dir():
        errors.append(f"{label} directory missing: {path_value}")


def is_generated_artifact(path_value: str) -> bool:
    """Return True for artifacts produced by build/test runs that are gitignored."""
    return path_value.startswith("experience/coverage/")


def validate_layer(root: Path, layer_name: str, layer: Dict, errors: List[str]) -> List[str]:
    if not isinstance(layer, dict):
        errors.append(f"Layer '{layer_name}' must be an object")
        return []

    status = str(layer.get("status", "")).strip().upper()
    if status != "PASS":
        errors.append(f"Layer '{layer_name}' must have status PASS (found: {status or 'missing'})")

    command = layer.get("command")
    if not isinstance(command, str) or not command.strip():
        errors.append(f"Layer '{layer_name}' must declare the executed command")

    artifacts = layer.get("artifacts")
    if not isinstance(artifacts, list) or not artifacts or not all(isinstance(item, str) for item in artifacts):
        errors.append(f"Layer '{layer_name}' must declare one or more artifact paths")
        return []

    for artifact in artifacts:
        if is_generated_artifact(artifact):
            continue  # declared but gitignored — skip disk check
        require_existing_file(root, f"Layer '{layer_name}'", artifact, errors)

    return artifacts


def validate_manifest(data: Dict, root: Path) -> List[str]:
    errors: List[str] = []

    require_string(data, "feature", errors)
    require_string(data, "recorded_on", errors)
    require_string(data, "runtime_path", errors)

    evidence_package = require_string(data, "evidence_package", errors)
    if evidence_package:
        require_existing_directory(root, "Evidence package", evidence_package, errors)

    for artifact_key in REQUIRED_TOP_LEVEL_ARTIFACTS:
        artifact_path = require_string(data, artifact_key, errors)
        if artifact_path:
            require_existing_file(root, artifact_key, artifact_path, errors)

    layers = data.get("layers")
    if not isinstance(layers, dict):
        errors.append("Missing required object field: layers")
        return errors

    layer_artifacts: Dict[str, List[str]] = {}
    for layer_name in REQUIRED_LAYERS:
        layer = layers.get(layer_name)
        if layer is None:
            errors.append(f"Missing required frontend layer: {layer_name}")
            continue
        layer_artifacts[layer_name] = validate_layer(root, layer_name, layer, errors)

    coverage_paths = {
        str(normalize_repo_path(path_value, root).relative_to(root))
        for path_value in layer_artifacts.get("coverage", [])
    }
    for generated_artifact in GENERATED_COVERAGE_ARTIFACTS:
        if generated_artifact not in coverage_paths:
            errors.append(
                "Coverage layer must declare generated artifact: "
                f"{generated_artifact}"
            )

    return errors


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate Nebula frontend quality evidence")
    parser.add_argument(
        "manifest",
        nargs="?",
        default=str(DEFAULT_MANIFEST),
        help="Path to the frontend quality manifest JSON",
    )
    args = parser.parse_args(list(argv) if argv is not None else None)

    root = repo_root()
    manifest_path = normalize_repo_path(args.manifest, root)

    try:
        manifest = read_manifest(manifest_path)
    except ValueError as exc:
        print(f"[FAIL] {exc}")
        return 1

    errors = validate_manifest(manifest, root)
    if errors:
        print("[FAIL] Frontend quality evidence is incomplete")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("[PASS] Frontend quality evidence manifest is complete")
    print(f"  manifest: {manifest_path.relative_to(root)}")
    for layer_name in REQUIRED_LAYERS:
        command = manifest["layers"][layer_name]["command"]
        print(f"  - {layer_name}: PASS ({command})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
