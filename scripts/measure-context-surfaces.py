#!/usr/bin/env python3
"""Measure high-token context surfaces for prompt-loading reports.

The estimate uses bytes / 4 as a rough token approximation for text prompt
context. Binary files are counted by bytes but should normally remain
on-demand-only and out of prompt context.

Usage:
    python3 scripts/measure-context-surfaces.py
    python3 scripts/measure-context-surfaces.py --format markdown
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path


SURFACES = {
    "features": ["planning-mds/features"],
    "feature_archive": ["planning-mds/features/archive"],
    "evidence": ["planning-mds/operations/evidence"],
    "architecture": ["planning-mds/architecture"],
    "api": ["planning-mds/api"],
    "lob_schemas": ["planning-mds/lob-schemas"],
    "knowledge_graph": ["planning-mds/knowledge-graph"],
    "backend": ["engine"],
    "frontend": ["experience"],
    "neuron": ["neuron"],
    "tests": ["engine/tests", "experience/tests", "neuron/tests", "scripts/tests"],
    "visual_generated_logs": [
        "planning-mds/operations/evidence",
        "experience/test-results",
        "experience/playwright-report",
    ],
}

BINARY_OR_VISUAL_SUFFIXES = {
    ".png",
    ".jpg",
    ".jpeg",
    ".gif",
    ".webp",
    ".mp4",
    ".mov",
    ".webm",
    ".har",
    ".log",
    ".trx",
    ".coverage",
    ".binlog",
}


@dataclass
class SurfaceMeasurement:
    name: str
    files: int = 0
    bytes: int = 0
    binary_or_visual_files: int = 0

    @property
    def estimated_tokens(self) -> int:
        return self.bytes // 4


def iter_files(root: Path) -> list[Path]:
    if not root.exists():
        return []
    if root.is_file():
        return [root]
    return [path for path in root.rglob("*") if path.is_file()]


def measure_surface(product_root: Path, name: str, paths: list[str]) -> SurfaceMeasurement:
    measurement = SurfaceMeasurement(name=name)
    seen: set[Path] = set()
    for relative in paths:
        for file_path in iter_files(product_root / relative):
            resolved = file_path.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            measurement.files += 1
            try:
                measurement.bytes += file_path.stat().st_size
            except OSError:
                continue
            if file_path.suffix.lower() in BINARY_OR_VISUAL_SUFFIXES:
                measurement.binary_or_visual_files += 1
    return measurement


def render_markdown(measurements: list[SurfaceMeasurement]) -> str:
    lines = [
        "| Surface | Files | Bytes | Est. Tokens | Binary/Visual/Log Files |",
        "|---|---:|---:|---:|---:|",
    ]
    for item in measurements:
        lines.append(
            f"| {item.name} | {item.files} | {item.bytes} | {item.estimated_tokens} | {item.binary_or_visual_files} |"
        )
    return "\n".join(lines)


def render_text(measurements: list[SurfaceMeasurement]) -> str:
    lines = []
    for item in measurements:
        lines.append(
            f"{item.name}: files={item.files} bytes={item.bytes} "
            f"estimated_tokens={item.estimated_tokens} binary_visual_log_files={item.binary_or_visual_files}"
        )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Measure high-token context surfaces")
    parser.add_argument("--format", choices=("text", "markdown"), default="text")
    args = parser.parse_args()

    product_root = Path.cwd()
    measurements = [
        measure_surface(product_root, name, paths)
        for name, paths in SURFACES.items()
    ]
    if args.format == "markdown":
        print(render_markdown(measurements))
    else:
        print(render_text(measurements))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
