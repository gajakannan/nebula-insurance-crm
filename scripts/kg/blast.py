#!/usr/bin/env python3
"""Compute blast radius for a file path or canonical node ID.

Given a starting point (repo file, ontology node, or feature/story), walk
the knowledge graph to enumerate all impacted surfaces: features, stories,
code bindings, Casbin policy rules, endpoints, UI routes, and migrations.

Usage:
    python3 scripts/kg/blast.py entity:submission
    python3 scripts/kg/blast.py --file engine/src/Nebula.Domain/Entities/Submission.cs
    python3 scripts/kg/blast.py F0007
    python3 scripts/kg/blast.py entity:renewal --compact
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from kg_common import (
    REF_FIELDS,
    edge_ref_id,
    edge_ref_ids,
    edge_ref_provenance,
    emit_telemetry,
    estimate_tokens,
    expand_declared_pattern,
    load_bundle,
    match_bindings_for_path,
    normalize_target_id,
    related_mapping_entries,
    repo_relative,
)

LOW_CONFIDENCE_THRESHOLD = 0.5


def node_ids_for_file(path: str, bundle: dict[str, Any]) -> set[str]:
    """Find all canonical node IDs bound to a file path via code-index."""
    return {m["id"] for m in match_bindings_for_path(path, bundle)}


def canonical_refs_from_mapping(node: dict[str, Any]) -> set[str]:
    """For a feature/story node, gather all canonical node IDs it references."""
    refs: set[str] = set()
    for field in REF_FIELDS:
        refs.update(edge_ref_ids(node.get(field, [])))
    return refs


def classify_mapping_edges(node: dict[str, Any]) -> tuple[str, int]:
    """Inspect edge provenance on a feature/story node.

    Returns (confidence_band, ambiguous_count) using the same vocabulary as
    scripts/kg/lookup.py so telemetry stays comparable across tools.
    """
    ambiguous_ids: set[str] = set()
    low = False
    medium = False
    for field in REF_FIELDS:
        for ref in node.get(field, []):
            prov = edge_ref_provenance(ref)
            if prov is None:
                continue
            provenance = prov.get("provenance")
            confidence = prov.get("confidence")
            if provenance == "ambiguous":
                ambiguous_ids.add(edge_ref_id(ref))
            elif provenance == "inferred":
                if isinstance(confidence, (int, float)) and confidence < LOW_CONFIDENCE_THRESHOLD:
                    low = True
                else:
                    medium = True
    if ambiguous_ids:
        return "ambiguous", len(ambiguous_ids)
    if low:
        return "low", 0
    if medium:
        return "medium", 0
    return "high", 0


def one_hop_neighbors(node_id: str, bundle: dict[str, Any]) -> set[str]:
    """Collect node IDs reachable in one hop via ref fields."""
    neighbors: set[str] = set()
    node = bundle["all_nodes"].get(node_id)
    if not node:
        return neighbors

    for field in REF_FIELDS:
        neighbors.update(edge_ref_ids(node.get(field, [])))

    if node.get("_kind") == "workflow":
        for state in node.get("states", []):
            neighbors.add(state["id"])

    wf_id = node.get("belongs_to_workflow")
    if wf_id:
        neighbors.add(wf_id)

    return neighbors


def find_related_policy_rules(
    node_ids: set[str], bundle: dict[str, Any]
) -> list[str]:
    """Find policy_rule nodes whose related_nodes intersect with node_ids."""
    rules: list[str] = []
    for rule in bundle["canonical"].get("policy_rules", []):
        related = set(rule.get("related_nodes", []))
        if related.intersection(node_ids):
            rules.append(rule["id"])
    return sorted(rules)


def flatten_paths(obj: Any) -> list[str]:
    """Recursively collect all string paths from a nested dict/list."""
    if isinstance(obj, str):
        return [obj]
    if isinstance(obj, list):
        result: list[str] = []
        for item in obj:
            result.extend(flatten_paths(item))
        return result
    if isinstance(obj, dict):
        result = []
        for v in obj.values():
            result.extend(flatten_paths(v))
        return result
    return []


def resolve_patterns(patterns: list[str]) -> list[str]:
    """Expand glob patterns to actual file paths."""
    resolved: list[str] = []
    for pattern in patterns:
        resolved.extend(expand_declared_pattern(pattern))
    return sorted(set(resolved))


def build_blast_report(
    starting_ids: set[str],
    bundle: dict[str, Any],
    query: dict[str, Any],
) -> dict[str, Any]:
    """Build the full blast radius report."""

    # One-hop expansion from starting nodes
    neighbor_ids: set[str] = set()
    for node_id in starting_ids:
        neighbor_ids |= one_hop_neighbors(node_id, bundle)
    neighbor_ids -= starting_ids

    all_impacted = starting_ids | neighbor_ids

    # Features and stories that reference direct nodes
    features, stories = related_mapping_entries(starting_ids, bundle["mappings"])

    # Indirect features/stories via neighbor nodes
    indirect_features, indirect_stories = related_mapping_entries(
        neighbor_ids, bundle["mappings"]
    )
    direct_feature_ids = {f["id"] for f in features}
    direct_story_ids = {s["id"] for s in stories}
    indirect_features = [
        f for f in indirect_features if f["id"] not in direct_feature_ids
    ]
    indirect_stories = [
        s for s in indirect_stories if s["id"] not in direct_story_ids
    ]

    # Code bindings for direct nodes
    direct_bindings: dict[str, dict[str, Any]] = {}
    for node_id in sorted(starting_ids):
        binding = bundle["bindings"].get(node_id)
        if binding:
            direct_bindings[node_id] = binding.get("paths", {})

    # Policy rules: nodes in impacted set + reverse lookup via related_nodes
    policy_rules_from_type = sorted(
        nid
        for nid in all_impacted
        if bundle["all_nodes"].get(nid, {}).get("_kind") == "policy_rule"
    )
    policy_rules_from_related = find_related_policy_rules(starting_ids, bundle)
    for feat in features:
        policy_rules_from_related.extend(edge_ref_ids(feat.get("enforced_by_policy", [])))
    policy_rules = sorted(
        set(policy_rules_from_type) | set(policy_rules_from_related)
    )

    # Categorize all impacted nodes by type
    impacted_by_type: dict[str, list[str]] = {}
    for nid in sorted(all_impacted):
        node = bundle["all_nodes"].get(nid)
        if node:
            kind = node.get("_kind", "unknown")
            impacted_by_type.setdefault(kind, []).append(nid)

    # Resolved file paths
    all_patterns: list[str] = []
    for paths_obj in direct_bindings.values():
        all_patterns.extend(flatten_paths(paths_obj))
    resolved_files = resolve_patterns(all_patterns)

    return {
        "query": query,
        "direct_nodes": sorted(starting_ids),
        "neighbor_nodes": sorted(neighbor_ids),
        "impacted_by_type": impacted_by_type,
        "features": [
            {"id": f["id"], "path": f.get("path"), "status": f.get("status")}
            for f in features
        ],
        "stories": [{"id": s["id"], "path": s.get("path")} for s in stories],
        "indirect_features": [
            {"id": f["id"], "path": f.get("path"), "status": f.get("status")}
            for f in indirect_features
        ],
        "indirect_stories": [
            {"id": s["id"], "path": s.get("path")} for s in indirect_stories
        ],
        "policy_rules": policy_rules,
        "code_bindings": direct_bindings,
        "resolved_files": resolved_files,
        "summary": {
            "direct_node_count": len(starting_ids),
            "neighbor_node_count": len(neighbor_ids),
            "feature_count": len(features),
            "indirect_feature_count": len(indirect_features),
            "story_count": len(stories),
            "indirect_story_count": len(indirect_stories),
            "policy_rule_count": len(policy_rules),
            "resolved_file_count": len(resolved_files),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compute blast radius for a file or canonical node ID."
    )
    parser.add_argument(
        "target",
        nargs="?",
        help="Node ID (entity:submission), feature (F0007), or story (F0007-S0003)",
    )
    parser.add_argument(
        "--file",
        dest="file_path",
        help="Repo file path (e.g. engine/src/Nebula.Domain/Entities/Submission.cs)",
    )
    parser.add_argument(
        "--compact",
        action="store_true",
        help="Output summary only, omit resolved file lists.",
    )
    parser.add_argument("--run-id", default=None, help="Correlation ID stamped onto emitted telemetry.")
    parser.add_argument(
        "--telemetry-file",
        type=Path,
        default=None,
        help="Append one JSONL telemetry event for this invocation.",
    )
    args = parser.parse_args()

    if not args.target and not args.file_path:
        parser.error("Provide a node ID or --file.")
    if args.target and args.file_path:
        parser.error("Use either a node ID or --file, not both.")

    bundle = load_bundle()

    confidence_band = "high"
    ambiguous_count = 0

    if args.file_path:
        starting_ids = node_ids_for_file(args.file_path, bundle)
        if not starting_ids:
            print(f"No KG bindings found for: {args.file_path}", file=sys.stderr)
            return 1
        query: dict[str, Any] = {"file": repo_relative(args.file_path)}
    else:
        normalized = normalize_target_id(args.target)
        node = bundle["all_nodes"].get(normalized)
        if node is None:
            print(f"Unknown node: {args.target}", file=sys.stderr)
            return 1

        if node.get("_kind") in ("feature", "story"):
            # For features/stories, blast from the canonical nodes they reference
            starting_ids = canonical_refs_from_mapping(node)
            if not starting_ids:
                starting_ids = {normalized}
            confidence_band, ambiguous_count = classify_mapping_edges(node)
            query = {
                "feature_or_story": normalized,
                "affected_canonical_nodes": sorted(starting_ids),
            }
        else:
            starting_ids = {normalized}
            query = {"node": normalized}

    report = build_blast_report(starting_ids, bundle, query)

    if args.compact:
        json.dump(report["summary"], sys.stdout, indent=2)
    else:
        json.dump(report, sys.stdout, indent=2)
    sys.stdout.write("\n")

    emit_telemetry(
        args.telemetry_file,
        args.run_id,
        "blast",
        {
            "query": query,
            "nodes_returned": report["direct_nodes"],
            "nodes_count": len(report["direct_nodes"]),
            "neighbor_nodes": report["neighbor_nodes"],
            "policy_rule_count": report["summary"]["policy_rule_count"],
            "resolved_file_count": report["summary"]["resolved_file_count"],
            "empty_scope": not report["direct_nodes"],
            "ambiguous_count": ambiguous_count,
            "hint_emitted": False,
            "confidence_band": confidence_band,
            "tokens_estimated": estimate_tokens(report if not args.compact else report["summary"]),
        },
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
