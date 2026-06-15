#!/usr/bin/env python3
"""Validate product-local prompt context loading policy."""

from __future__ import annotations

import argparse
import fnmatch
import sys
from pathlib import Path
from typing import Any

import yaml


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

ROUTING_MODES = {"exact_file", "target_feature", "kg_lookup", "changed_path", "manifest", "explicit_user"}

FORBIDDEN_DEFAULT_PATTERNS = [
    "planning-mds/features/**",
    "planning-mds/features/archive/**",
    "planning-mds/operations/evidence/**",
    "planning-mds/operations/evidence/runs/**",
    "planning-mds/operations/evidence/**/artifacts/**",
    "planning-mds/architecture/**",
    "planning-mds/api/**",
    "planning-mds/schemas/**",
    "planning-mds/lob-schemas/**",
    "planning-mds/knowledge-graph/**",
    "engine/**",
    "engine/src/**",
    "engine/tests/**",
    "experience/**",
    "experience/src/**",
    "experience/tests/**",
    "experience/node_modules/**",
    "experience/dist/**",
    "experience/test-results/**",
    "experience/playwright-report/**",
    "experience/coverage/**",
    "neuron/**",
    "planning-mds/examples/**",
    "planning-mds/screens/**",
    "**/*.log",
    "**/*.png",
    "**/*.jpg",
    "**/*.jpeg",
    "**/*.gif",
    "**/*.webp",
    "**/*.pdf",
]

SOURCE_TREE_GLOBS = {"engine/**", "engine/src/**", "engine/tests/**", "experience/**", "experience/src/**", "experience/tests/**", "neuron/**"}
VISUAL_OR_GENERATED_GLOBS = {"**/*.log", "**/*.png", "**/*.jpg", "**/*.jpeg", "**/*.gif", "**/*.webp", "**/*.pdf"}


def normalize(value: Any) -> list[str]:
    if value is None:
        return []
    if not isinstance(value, list):
        return []
    return [str(item) for item in value]


def is_forbidden_default(path: str) -> str | None:
    for pattern in FORBIDDEN_DEFAULT_PATTERNS:
        if path == pattern or fnmatch.fnmatch(path, pattern):
            # Allow small index entry points that intentionally live under
            # otherwise large trees.
            if path in {
                "planning-mds/features/REGISTRY.md",
                "planning-mds/features/ROADMAP.md",
                "planning-mds/operations/evidence/README.md",
                "planning-mds/operations/evidence/features/**/latest-run.json",
            }:
                return None
            if path.startswith("scripts/kg/"):
                return None
            return pattern
    return None


def validate_context_map(path: Path) -> list[str]:
    errors: list[str] = []
    if not path.exists():
        return [f"context map not found: {path}"]

    try:
        data = yaml.safe_load(path.read_text(encoding="utf-8"))
    except yaml.YAMLError as exc:
        return [f"{path}: YAML parse failed: {exc}"]

    if not isinstance(data, dict):
        return [f"{path}: top-level document must be a mapping"]

    layers = data.get("layers")
    if not isinstance(layers, dict):
        return [f"{path}: missing top-level 'layers' mapping"]

    missing = sorted(REQUIRED_LAYERS - set(layers))
    if missing:
        errors.append(f"missing required layers: {', '.join(missing)}")

    modes = data.get("routing_modes")
    if not isinstance(modes, dict):
        errors.append("missing top-level 'routing_modes' mapping")
    else:
        missing_modes = sorted(ROUTING_MODES - set(modes))
        if missing_modes:
            errors.append(f"missing routing modes: {', '.join(missing_modes)}")

    for layer_name, layer in layers.items():
        if not isinstance(layer, dict):
            errors.append(f"layer {layer_name}: must be a mapping")
            continue

        default_context = normalize(layer.get("default_context"))
        on_demand_context = normalize(layer.get("on_demand_context"))
        routing_required = set(normalize(layer.get("routing_required")))

        for entry in default_context:
            forbidden = is_forbidden_default(entry)
            if forbidden:
                errors.append(
                    f"layer {layer_name}: default_context entry '{entry}' matches forbidden broad/default pattern '{forbidden}'"
                )

        if on_demand_context and not routing_required:
            errors.append(f"layer {layer_name}: on_demand_context requires routing_required")

        unknown_routes = sorted(routing_required - ROUTING_MODES)
        if unknown_routes:
            errors.append(f"layer {layer_name}: unknown routing modes: {', '.join(unknown_routes)}")

        if layer_name in {"archive_scope", "evidence_scope", "api_schema_scope", "frontend_scope", "backend_scope", "neuron_scope", "examples"}:
            if not routing_required.intersection({"exact_file", "kg_lookup", "changed_path", "manifest", "explicit_user"}):
                errors.append(
                    f"layer {layer_name}: on-demand layer must require exact-file, KG, changed-path, manifest, or explicit-user routing"
                )

    feature_scope = layers.get("feature_scope", {})
    if isinstance(feature_scope, dict):
        if feature_scope.get("target_scoped") is not True:
            errors.append("feature_scope: target_scoped must be true")
        for entry in normalize(feature_scope.get("default_context")):
            if entry == "planning-mds/features/**" or entry.startswith("planning-mds/features/archive"):
                errors.append(f"feature_scope: broad feature/archive default is forbidden: {entry}")

    for layer_name, layer in layers.items():
        if not isinstance(layer, dict):
            continue
        for entry in normalize(layer.get("default_context")):
            if entry in SOURCE_TREE_GLOBS:
                errors.append(f"layer {layer_name}: source tree cannot be default prompt context: {entry}")
            if entry in VISUAL_OR_GENERATED_GLOBS or any(fnmatch.fnmatch(entry, pattern) for pattern in VISUAL_OR_GENERATED_GLOBS):
                errors.append(f"layer {layer_name}: generated/visual/log artifact cannot be default prompt context: {entry}")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate planning-mds/context-map.yaml")
    parser.add_argument("--path", default="planning-mds/context-map.yaml", help="Context map path")
    args = parser.parse_args()

    errors = validate_context_map(Path(args.path))
    if errors:
        print("[FAIL] context map validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print(f"[PASS] context map is valid: {args.path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
