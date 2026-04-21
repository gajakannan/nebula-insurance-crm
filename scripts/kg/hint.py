#!/usr/bin/env python3
"""Output KG routing hints for a file or directory path.

Agent-agnostic CLI tool. Given a repo-relative path, looks up code-index
bindings and outputs a compact summary of matched nodes, features, stories,
and Casbin policy rules.

Usage:
    python3 scripts/kg/hint.py engine/src/Nebula.Domain/Entities/Submission.cs
    python3 scripts/kg/hint.py engine/src/Nebula.Domain/Entities
    python3 scripts/kg/hint.py experience/src/features/renewals
    python3 scripts/kg/hint.py --json engine/src/Nebula.Domain/Entities/Renewal.cs

Exit code is always 0 (advisory). Produces no output when no bindings match.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from kg_common import (
    emit_telemetry,
    estimate_tokens,
    load_bundle,
    match_bindings_for_path,
    normalize_repo_path,
    related_mapping_entries,
)


def find_policy_rules_for_nodes(
    node_ids: list[str], bundle: dict[str, Any]
) -> list[str]:
    """Find policy_rule nodes whose related_nodes mention any of node_ids."""
    wanted = set(node_ids)
    rules: list[str] = []
    for rule in bundle["canonical"].get("policy_rules", []):
        related = set(rule.get("related_nodes", []))
        if related.intersection(wanted):
            rules.append(rule["id"])
    return sorted(rules)


def match_path(normalized: str, bundle: dict[str, Any]) -> list[dict[str, Any]]:
    """Match a path against code-index bindings, handling files and directories."""
    matches = match_bindings_for_path(normalized, bundle)

    # If no direct match and path looks like a directory, try prefix matching
    if not matches and not Path(normalized).suffix:
        prefix = normalized.rstrip("/") + "/"
        for binding in bundle["bindings"].values():
            for entry in binding.get("declared_paths", []):
                if entry["pattern"].startswith(prefix):
                    matches.append(binding)
                    break
        seen: set[str] = set()
        deduped = []
        for m in matches:
            if m["id"] not in seen:
                seen.add(m["id"])
                deduped.append(m)
        matches = deduped

    return matches


def format_text(
    path: str,
    node_ids: list[str],
    features: list[dict[str, Any]],
    stories: list[dict[str, Any]],
    policy_rules: list[str],
) -> str:
    """Format KG hints as compact human-readable text."""
    lines = [f"[KG] {path} -> {', '.join(node_ids)}"]

    if features:
        feat_parts = [f["id"] for f in features[:6]]
        lines.append(f"  Features: {', '.join(feat_parts)}")

    if stories:
        story_parts = [s["id"] for s in stories[:8]]
        suffix = f" (+{len(stories) - 8} more)" if len(stories) > 8 else ""
        lines.append(f"  Stories: {', '.join(story_parts)}{suffix}")

    if policy_rules:
        lines.append(f"  Casbin: {', '.join(policy_rules[:6])}")

    lines.append(
        "  Tip: `python3 scripts/kg/blast.py --file <path>` for full blast radius"
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Output KG routing hints for a file or directory path."
    )
    parser.add_argument(
        "path",
        help="Repo-relative file or directory path",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        dest="as_json",
        help="Output as JSON instead of human-readable text.",
    )
    parser.add_argument("--run-id", default=None, help="Correlation ID stamped onto emitted telemetry.")
    parser.add_argument(
        "--telemetry-file",
        type=Path,
        default=None,
        help="Append one JSONL telemetry event for this invocation.",
    )
    args = parser.parse_args()

    normalized = normalize_repo_path(args.path)

    if normalized.count("/") < 2:
        return 0

    try:
        bundle = load_bundle()
    except SystemExit:
        return 0

    matches = match_path(normalized, bundle)
    if not matches:
        emit_telemetry(
            args.telemetry_file,
            args.run_id,
            "hint",
            {
                "path": normalized,
                "nodes_returned": [],
                "nodes_count": 0,
                "empty_scope": True,
                "ambiguous_count": 0,
                "hint_emitted": False,
                "confidence_band": "low",
                "tokens_estimated": 1,
            },
        )
        return 0

    node_ids = [m["id"] for m in matches]
    features, stories = related_mapping_entries(node_ids, bundle["mappings"])
    policy_rules = find_policy_rules_for_nodes(node_ids, bundle)

    if args.as_json:
        payload = {
            "path": normalized,
            "nodes": node_ids,
            "features": [f["id"] for f in features],
            "stories": [s["id"] for s in stories],
            "policy_rules": policy_rules,
        }
        json.dump(payload, sys.stdout, indent=2)
        sys.stdout.write("\n")
    else:
        print(format_text(normalized, node_ids, features, stories, policy_rules))

    emit_telemetry(
        args.telemetry_file,
        args.run_id,
        "hint",
        {
            "path": normalized,
            "nodes_returned": node_ids,
            "nodes_count": len(node_ids),
            "feature_ids": [f["id"] for f in features],
            "story_ids": [s["id"] for s in stories],
            "policy_rule_ids": policy_rules,
            "empty_scope": False,
            "ambiguous_count": 0,
            "hint_emitted": False,
            "confidence_band": "high",
            "tokens_estimated": estimate_tokens(
                {
                    "path": normalized,
                    "nodes": node_ids,
                    "features": [f["id"] for f in features],
                    "stories": [s["id"] for s in stories],
                    "policy_rules": policy_rules,
                }
            ),
        },
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
