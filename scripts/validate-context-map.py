#!/usr/bin/env python3
"""Validate product-local prompt context-loading strategy.

Usage:
    python3 scripts/validate-context-map.py
    python3 scripts/validate-context-map.py --context-map planning-mds/context-map.yaml
"""

from __future__ import annotations

import argparse
import fnmatch
import sys
from pathlib import Path
from typing import Any

try:
    import yaml
except ImportError:  # pragma: no cover - environment diagnostic
    yaml = None


REQUIRED_LAYERS = {
    "product_core",
    "feature_scope",
    "architecture_scope",
    "knowledge_graph_scope",
    "api_schema_scope",
    "frontend_scope",
    "backend_scope",
    "neuron_scope",
    "evidence_scope",
    "archive_scope",
    "output_format",
    "examples",
}

BROAD_DEFAULT_PATTERNS = [
    "planning-mds/features/**",
    "planning-mds/features/archive/**",
    "planning-mds/operations/evidence/**",
    "planning-mds/api/**",
    "planning-mds/api/*.yaml",
    "planning-mds/lob-schemas/**",
    "engine/**",
    "engine/src/**",
    "engine/tests/**",
    "experience/**",
    "experience/src/**",
    "experience/tests/**",
    "neuron/**",
    "**/screenshots/**",
    "**/visual-evidence/**",
    "**/artifacts/**",
    "**/test-results/**",
    "**/coverage/**",
    "**/*.png",
    "**/*.jpg",
    "**/*.jpeg",
    "**/*.gif",
    "**/*.webp",
    "**/*.mp4",
    "**/*.har",
    "**/*.log",
]

ALLOWED_DEFAULT_PATHS = {
    "README.md",
    "lifecycle-stage.yaml",
    ".agentignore",
    "planning-mds/context-map.yaml",
    "planning-mds/BLUEPRINT.md",
    "planning-mds/features/REGISTRY.md",
    "planning-mds/features/ROADMAP.md",
    "planning-mds/knowledge-graph/README.md",
}

ON_DEMAND_LAYERS = {
    "architecture_scope",
    "api_schema_scope",
    "frontend_scope",
    "backend_scope",
    "neuron_scope",
    "evidence_scope",
    "archive_scope",
    "examples",
}

ALLOWED_ROUTING_TERMS = {
    "exact",
    "kg",
    "changed",
    "explicit",
    "target",
    "manifest",
    "topic",
    "contract",
    "schema",
}


def load_yaml(path: Path) -> dict[str, Any]:
    if yaml is None:
        raise RuntimeError("PyYAML is required: install pyyaml or run in the project tool environment")
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError("context map must parse to a YAML mapping")
    return data


def as_list(value: Any) -> list[Any]:
    if value is None:
        return []
    if isinstance(value, list):
        return value
    return [value]


def paths_from_layer(layer: dict[str, Any], keys: tuple[str, ...] = ("load", "load_on_demand")) -> list[str]:
    paths: list[str] = []
    for key in keys:
        for item in as_list(layer.get(key)):
            if isinstance(item, str):
                paths.append(item)
            elif isinstance(item, dict) and isinstance(item.get("path"), str):
                paths.append(item["path"])
    for item in as_list(layer.get("conditional")):
        if isinstance(item, dict) and isinstance(item.get("path"), str):
            paths.append(item["path"])
    return paths


def matches_broad_default(path: str) -> bool:
    normalized = path.strip()
    if normalized in ALLOWED_DEFAULT_PATHS or "{FEATURE_ID}" in normalized:
        return False
    return any(
        fnmatch.fnmatch(normalized, pattern) or normalized == pattern
        for pattern in BROAD_DEFAULT_PATTERNS
    )


def routing_is_safe(value: Any) -> bool:
    if not isinstance(value, str):
        return False
    lowered = value.lower()
    return any(term in lowered for term in ALLOWED_ROUTING_TERMS)


def validate_context_map(data: dict[str, Any]) -> list[str]:
    errors: list[str] = []

    layers = data.get("layers")
    if not isinstance(layers, dict):
        return ["missing or invalid top-level 'layers' mapping"]

    missing = sorted(REQUIRED_LAYERS - set(layers))
    if missing:
        errors.append(f"missing required layers: {', '.join(missing)}")

    default_layers = as_list(data.get("default_prompt_context", {}).get("layers"))
    for layer_name in default_layers:
        layer = layers.get(layer_name)
        if not isinstance(layer, dict):
            errors.append(f"default layer '{layer_name}' is not defined")
            continue
        if layer.get("default") is not True:
            errors.append(f"default layer '{layer_name}' must set default: true")

    for layer_name, raw_layer in layers.items():
        if not isinstance(raw_layer, dict):
            errors.append(f"layer '{layer_name}' must be a mapping")
            continue

        is_default = raw_layer.get("default") is True
        default_paths = paths_from_layer(raw_layer, keys=("load",))

        if is_default:
            for path in default_paths:
                if matches_broad_default(path) and "{FEATURE_ID}" not in path:
                    errors.append(
                        f"default layer '{layer_name}' includes broad/high-token path: {path}"
                    )

        if layer_name == "feature_scope":
            for path in default_paths:
                if path.startswith("planning-mds/features/") and "{FEATURE_ID}" not in path:
                    errors.append(
                        f"feature_scope must be target-scoped with {{FEATURE_ID}}: {path}"
                    )

        if layer_name in ON_DEMAND_LAYERS:
            if raw_layer.get("default") is True:
                errors.append(f"on-demand layer '{layer_name}' cannot be default")
            if not routing_is_safe(raw_layer.get("routing")):
                errors.append(
                    f"on-demand layer '{layer_name}' must require exact-file, KG, changed-path, target, manifest, or explicit-user routing"
                )

    never_default = as_list(data.get("never_default_prompt_context"))
    for path in never_default:
        if isinstance(path, str) and not matches_broad_default(path):
            continue
    if not never_default:
        errors.append("missing never_default_prompt_context list")

    blocked_defaults = {
        path
        for layer_name, layer in layers.items()
        if isinstance(layer, dict) and layer.get("default") is True
        for path in paths_from_layer(layer, keys=("load",))
        if isinstance(path, str)
    }
    for artifact_pattern in ("**/*.png", "**/*.log", "**/screenshots/**", "planning-mds/operations/evidence/**"):
        if artifact_pattern in blocked_defaults:
            errors.append(f"generated/visual/log artifact is default prompt context: {artifact_pattern}")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate planning-mds/context-map.yaml")
    parser.add_argument(
        "--context-map",
        default="planning-mds/context-map.yaml",
        help="Path to context-map.yaml relative to product root",
    )
    args = parser.parse_args()

    product_root = Path.cwd()
    context_map = product_root / args.context_map
    if not context_map.exists():
        print(f"[FAIL] context map not found: {context_map}")
        return 1

    try:
        data = load_yaml(context_map)
    except Exception as exc:
        print(f"[FAIL] context map does not parse: {exc}")
        return 1

    errors = validate_context_map(data)
    if errors:
        print("[FAIL] context map validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("[PASS] context map validation passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
