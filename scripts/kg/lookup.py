#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

from kg_common import (
    KG_DIR,
    REF_FIELDS,
    edge_ref_id,
    edge_ref_ids,
    edge_ref_provenance,
    emit_telemetry,
    estimate_tokens,
    feature_or_story_by_id,
    get_symbol_by_id,
    load_bundle,
    load_yaml,
    match_decisions_for_path,
    match_decisions_for_symbol,
    match_bindings_for_path,
    match_symbol_by_name,
    match_symbols_for_node,
    normalize_target_id,
    planning_scope_for_path,
    related_mapping_entries,
    repo_relative,
    resolve_node,
    resolve_refs,
)


HOTSPOT_FIELDS = (
    "hotspot_rank",
    "hotspot_score",
    "primary_owner",
    "primary_owner_pct",
    "bus_factor_flag",
)

_HOTSPOT_CACHE: dict[str, dict[str, Any]] | None = None


def _hotspots_by_node() -> dict[str, dict[str, Any]]:
    """Lazy-load Phase 3 freshness fields from coverage-report.yaml.

    Returns a dict {node_id: {hotspot_rank, hotspot_score, primary_owner,
    primary_owner_pct, bus_factor_flag}} for nodes that carry the fields.
    Missing report or missing fields → empty dict (Phase 3 is additive).
    """
    global _HOTSPOT_CACHE
    if _HOTSPOT_CACHE is not None:
        return _HOTSPOT_CACHE
    report_path = KG_DIR / "coverage-report.yaml"
    if not report_path.exists():
        _HOTSPOT_CACHE = {}
        return _HOTSPOT_CACHE
    report = load_yaml(report_path) or {}
    canonical = report.get("freshness", {}).get("canonical", {}) or {}
    out: dict[str, dict[str, Any]] = {}
    for node_id, entry in canonical.items():
        slice_ = {k: entry[k] for k in HOTSPOT_FIELDS if k in entry}
        if slice_:
            out[node_id] = slice_
    _HOTSPOT_CACHE = out
    return out


def _attach_hotspots(node: dict[str, Any]) -> dict[str, Any]:
    """Annotate a resolved node payload with Phase 3 fields when available."""
    node_id = node.get("id") if isinstance(node, dict) else None
    if not node_id:
        return node
    fields = _hotspots_by_node().get(node_id)
    if not fields:
        return node
    annotated = dict(node)
    annotated["hotspots"] = fields
    return annotated

FEATURE_OR_STORY_ID_RE = re.compile(r"^(?:feature:)?F\d{4}$|^(?:story:)?F\d{4}-S\d{4}$")
LOW_CONFIDENCE_THRESHOLD = 0.5


def _include_label(node: dict[str, Any], summary: dict[str, Any]) -> None:
    label = node.get("label")
    if label:
        summary["label"] = label


def summarize_node(
    node: dict[str, Any],
    tier: int,
    fields: str,
) -> dict[str, Any]:
    summary: dict[str, Any] = {"id": node["id"]}
    _include_label(node, summary)

    if tier >= 2 and fields in {"summaries", "full"}:
        if node.get("synopsis"):
            summary["synopsis"] = node["synopsis"]
        if node.get("notes"):
            summary["notes"] = node["notes"]
        if node.get("rationale"):
            summary["rationale"] = node["rationale"]

    if tier >= 3 and fields == "full":
        if node.get("path"):
            summary["path"] = node["path"]
        if node.get("source_docs"):
            summary["source_docs"] = node["source_docs"]

        linked_adr_ids = sorted(
            {
                entry["adr"]
                for entry in node.get("rationale", [])
                if isinstance(entry, dict) and entry.get("adr")
            }
        )
        related_nodes = node.get("related_nodes", [])
        linked_schema_ids = sorted(
            ref_id for ref_id in related_nodes if ref_id.startswith("schema:")
        )
        linked_policy_rule_ids = sorted(
            ref_id for ref_id in related_nodes if ref_id.startswith("policy_rule:")
        )
        if linked_adr_ids:
            summary["linked_adr_ids"] = linked_adr_ids
        if linked_schema_ids:
            summary["linked_schema_ids"] = linked_schema_ids
        if linked_policy_rule_ids:
            summary["linked_policy_rule_ids"] = linked_policy_rule_ids

    # Phase 3 freshness fields are routing-aid metadata. Gate them the same
    # way rationale/source_docs are gated so tier 1 / fields=ids stay minimal.
    if tier >= 2 and fields in {"summaries", "full"}:
        hotspots = _hotspots_by_node().get(node["id"])
        if hotspots:
            summary["hotspots"] = hotspots

    return summary


def summarize_scope_payload(
    payload: dict[str, Any],
    tier: int,
    fields: str,
) -> dict[str, Any]:
    if tier == 4 and fields == "full":
        return payload

    summarized = dict(payload)
    if "feature" in summarized and isinstance(summarized["feature"], dict):
        if tier == 4:
            summarized["feature"] = summarize_node(summarized["feature"], 3, fields)
        else:
            summarized["feature"] = summarize_node(summarized["feature"], tier, fields)

    for field in REF_FIELDS:
        refs = payload.get(field)
        if not refs:
            continue
        if tier == 4:
            summarized[field] = [
                summarize_node(node, 3, fields) if fields != "full" else node
                for node in refs
            ]
            continue
        summarized[field] = [summarize_node(node, tier, fields) for node in refs]

    return summarized


def summarize_file_payload(payload: dict[str, Any], fields: str) -> dict[str, Any]:
    if fields == "full":
        return payload

    summarized = dict(payload)
    summarized["matched_nodes"] = [
        summarize_node(node, 3, fields)
        for node in payload.get("matched_nodes", [])
        if isinstance(node, dict)
    ]
    return summarized


def find_low_confidence_refs(payload: dict[str, Any]) -> list[str]:
    flagged: list[str] = []
    for entries in payload.get("provenance", {}).values():
        for entry in entries:
            provenance = entry.get("provenance")
            confidence = entry.get("confidence")
            if provenance == "ambiguous":
                flagged.append(entry["id"])
            elif provenance == "inferred" and isinstance(confidence, (int, float)) and confidence < LOW_CONFIDENCE_THRESHOLD:
                flagged.append(entry["id"])
    return sorted(set(flagged))


def append_lookup_hints(payload: dict[str, Any], tier: int) -> dict[str, Any]:
    if tier not in (1, 2):
        return payload

    flagged = find_low_confidence_refs(payload)
    if not flagged:
        return payload

    hint = (
        f"{len(flagged)} ambiguous nodes detected ({', '.join(flagged)}) "
        "-- consider --tier 3 or open source_docs directly"
    )
    hinted = dict(payload)
    hinted["hints"] = [hint]
    return hinted


def confidence_band(payload: dict[str, Any]) -> str:
    if payload.get("scope") is None and payload.get("reason") == "unmapped":
        return "low"

    ambiguous = False
    low = False
    medium = False
    for entries in payload.get("provenance", {}).values():
        for entry in entries:
            provenance = entry.get("provenance")
            confidence = entry.get("confidence")
            if provenance == "ambiguous":
                ambiguous = True
            elif provenance == "inferred":
                if isinstance(confidence, (int, float)) and confidence < LOW_CONFIDENCE_THRESHOLD:
                    low = True
                else:
                    medium = True
    if ambiguous:
        return "ambiguous"
    if low:
        return "low"
    if medium:
        return "medium"
    return "high"


def collect_returned_node_ids(payload: dict[str, Any]) -> list[str]:
    returned: list[str] = []
    for field in REF_FIELDS:
        for entry in payload.get(field, []):
            if isinstance(entry, dict) and entry.get("id"):
                returned.append(entry["id"])
    return sorted(dict.fromkeys(returned))


def emit_lookup_telemetry(
    telemetry_file: Path | None,
    run_id: str | None,
    payload: dict[str, Any],
    tier_requested: int | None,
    tier_returned: int | None,
    file_path: str | None = None,
    symbol_name: str | None = None,
) -> None:
    if symbol_name:
        matches = payload.get("matches", []) or []
        symbol_ids = [m["symbol"]["id"] for m in matches if m.get("symbol")]
        nodes_returned = sorted({m["symbol"]["node"] for m in matches if m.get("symbol")})
        empty_scope = not symbol_ids
        event: dict[str, Any] = {
            "tier_requested": None,
            "tier_returned": None,
            "nodes_returned": nodes_returned,
            "nodes_count": len(nodes_returned),
            "symbols_returned": symbol_ids,
            "symbols_count": len(symbol_ids),
            "ambiguous_count": 0,
            "empty_scope": empty_scope,
            "hint_emitted": bool(payload.get("hints")),
            "confidence_band": "low" if empty_scope else "high",
            "tokens_estimated": estimate_tokens(payload),
            "query_symbol": symbol_name,
        }
        emit_telemetry(telemetry_file, run_id, "lookup", event)
        return

    nodes_returned = (
        payload.get("matched_node_ids", [])
        if file_path
        else collect_returned_node_ids(payload)
    )
    ambiguous_count = len(find_low_confidence_refs(payload))
    hint_emitted = bool(payload.get("hints"))
    event = {
        "tier_requested": tier_requested,
        "tier_returned": tier_returned,
        "nodes_returned": nodes_returned,
        "nodes_count": len(nodes_returned),
        "ambiguous_count": ambiguous_count,
        "empty_scope": bool(payload.get("scope") is None or not nodes_returned),
        "hint_emitted": hint_emitted,
        "confidence_band": confidence_band(payload),
        "tokens_estimated": estimate_tokens(payload),
    }
    if file_path:
        event["query_file"] = file_path
    emit_telemetry(telemetry_file, run_id, "lookup", event)


def build_scope_payload(target: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "target": target,
        "source_precedence": bundle["ontology"]["authority"]["precedence"],
    }

    if target.get("_kind") == "story" and target.get("feature"):
        feature = resolve_node(target["feature"], bundle)
        if feature is not None:
            payload["feature"] = feature

    provenance_annotations: dict[str, list[dict[str, Any]]] = {}

    for field in REF_FIELDS:
        refs = target.get(field, [])
        if refs:
            ref_ids = edge_ref_ids(refs)
            payload[field] = resolve_refs(ref_ids, bundle)
            # Collect provenance for edges that have it
            for ref in refs:
                prov = edge_ref_provenance(ref)
                if prov is not None:
                    provenance_annotations.setdefault(field, []).append(
                        {"id": edge_ref_id(ref), **prov}
                    )

    if provenance_annotations:
        payload["provenance"] = provenance_annotations

    return payload


def unmapped_payload(target: str, normalized: str) -> dict[str, Any]:
    scope_key = "story_id" if normalized.startswith("story:") else "feature_id"
    return {
        scope_key: target.strip(),
        "scope": None,
        "reason": "unmapped",
        "hints": [
            "Feature has no mapping in feature-mappings.yaml; proceed file-centric; seed stub before Phase B"
        ],
    }


def lookup_by_target(
    target: str,
    bundle: dict[str, Any],
    *,
    tier: int,
    fields: str,
    allow_missing: bool,
) -> dict[str, Any]:
    normalized = normalize_target_id(target)

    scope = feature_or_story_by_id(normalized, bundle["mappings"])
    if scope is not None:
        scope["_kind"] = "feature" if normalized.startswith("feature:") else "story"
        payload = build_scope_payload(scope, bundle)
        payload = summarize_scope_payload(payload, tier, fields)
        return append_lookup_hints(payload, tier)

    if allow_missing and FEATURE_OR_STORY_ID_RE.fullmatch(normalized):
        return unmapped_payload(target, normalized)

    node = resolve_node(normalized, bundle)
    if node is None:
        raise SystemExit(f"Unknown target: {target}")

    related_features, related_stories = related_mapping_entries(
        [normalized], bundle["mappings"]
    )
    target_payload = (
        summarize_node(node, min(tier, 3), fields) if fields != "full" else node
    )
    payload = {
        "target": _attach_hotspots(target_payload),
        "related_features": related_features,
        "related_stories": related_stories,
        "source_precedence": bundle["ontology"]["authority"]["precedence"],
    }
    return append_lookup_hints(payload, tier)


def _summarize_symbol_brief(symbol: dict[str, Any]) -> dict[str, Any]:
    """Compact symbol view for sibling/edge lists in lookup output."""
    return {
        "id": symbol["id"],
        "name": symbol.get("name"),
        "kind": symbol.get("kind"),
        "container": symbol.get("container"),
        "file": symbol.get("file"),
        "line": symbol.get("line"),
        "language": symbol.get("language"),
    }


def lookup_by_symbol(
    name: str, bundle: dict[str, Any], *, node_id: str | None = None
) -> dict[str, Any]:
    """Resolve a symbol by source name, optionally scoped to one canonical node.

    Returns the full record(s), brief callers/callees, and sibling symbols on
    the same canonical node. The raw source file at file:line remains
    authoritative; this payload is a routing aid only.
    """
    matches = match_symbol_by_name(name, bundle, node_id=node_id)
    payload: dict[str, Any] = {
        "query": {"symbol_name": name, "node": node_id},
        "matched_count": len(matches),
        "matches": [],
        "source_precedence": bundle["ontology"]["authority"]["precedence"],
    }
    if not matches:
        payload["hints"] = [
            "No symbols with this name in symbol-index.yaml; raw source files "
            "remain authoritative — try `hint.py` or `grep`."
        ]
        return payload

    for symbol in matches:
        callers = [
            _summarize_symbol_brief(s)
            for cid in symbol.get("callers", [])
            for s in [get_symbol_by_id(cid, bundle)]
            if s is not None
        ]
        callees = [
            _summarize_symbol_brief(s)
            for cid in symbol.get("callees", [])
            for s in [get_symbol_by_id(cid, bundle)]
            if s is not None
        ]
        siblings = [
            _summarize_symbol_brief(s)
            for s in match_symbols_for_node(symbol["node"], bundle)
            if s["id"] != symbol["id"]
            and s.get("file") == symbol.get("file")
        ]
        payload["matches"].append(
            {
                "symbol": symbol,
                "callers": callers,
                "callees": callees,
                "decisions": match_decisions_for_symbol(symbol["id"], bundle),
                "siblings_in_file": siblings[:25],
                "siblings_truncated": max(0, len(siblings) - 25),
            }
        )
    return payload


def lookup_by_file(path: str, bundle: dict[str, Any]) -> dict[str, Any]:
    binding_matches = match_bindings_for_path(path, bundle)
    node_ids = [match["id"] for match in binding_matches]
    planning_scope = planning_scope_for_path(path, bundle["mappings"])
    related_features, related_stories = related_mapping_entries(node_ids, bundle["mappings"])

    matched_nodes = [
        _attach_hotspots(resolve_node(node_id, bundle))
        for node_id in node_ids
    ]

    return {
        "query": {"file": repo_relative(path)},
        "matched_node_ids": node_ids,
        "matched_nodes": matched_nodes,
        "planning_scope": planning_scope,
        "related_features": related_features,
        "related_stories": related_stories,
        "matched_bindings": [
            {
                "id": match["id"],
                "matched_patterns": match["matched_patterns"],
                "paths": match.get("paths", {}),
            }
            for match in binding_matches
        ],
        "decisions": match_decisions_for_path(path, bundle),
        "source_precedence": bundle["ontology"]["authority"]["precedence"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Resolve ontology-backed planning scope and code bindings. Raw artifacts remain the source of truth."
    )
    parser.add_argument("target", nargs="?", help="Feature/story ID such as F0007 or F0007-S0003")
    parser.add_argument(
        "--file",
        dest="file_path",
        help="Reverse lookup for a repo file path such as engine/src/.../Submission.cs",
    )
    parser.add_argument(
        "--symbol",
        dest="symbol_name",
        help="Look up a symbol by source name (e.g. TransitionAsync). Uses symbol-index.yaml.",
    )
    parser.add_argument(
        "--node",
        dest="symbol_node",
        help="Scope --symbol lookups to a canonical node id (e.g. entity:submission).",
    )
    parser.add_argument(
        "--tier",
        type=int,
        choices=[1, 2, 3, 4],
        default=4,
        help="Lookup depth for feature/story scope output. Tier 4 preserves current one-hop expansion.",
    )
    parser.add_argument(
        "--fields",
        choices=["ids", "summaries", "full"],
        default="full",
        help="Verbosity of resolved node summaries within the selected tier.",
    )
    parser.add_argument(
        "--allow-missing",
        action="store_true",
        help="Return an unmapped payload for missing feature/story mappings instead of exiting non-zero.",
    )
    parser.add_argument("--run-id", default=None, help="Correlation ID stamped onto emitted telemetry.")
    parser.add_argument(
        "--telemetry-file",
        type=Path,
        default=None,
        help="Append one JSONL telemetry event for this invocation.",
    )
    args = parser.parse_args()

    modes = sum(bool(x) for x in (args.target, args.file_path, args.symbol_name))
    if modes == 0:
        parser.error("Provide a target ID, --file, or --symbol.")
    if modes > 1:
        parser.error("Use only one of: target ID, --file, --symbol.")

    bundle = load_bundle()
    if args.symbol_name:
        payload = lookup_by_symbol(
            args.symbol_name, bundle, node_id=args.symbol_node
        )
    elif args.file_path:
        payload = summarize_file_payload(
            lookup_by_file(args.file_path, bundle), args.fields
        )
    else:
        payload = lookup_by_target(
            args.target,
            bundle,
            tier=args.tier,
            fields=args.fields,
            allow_missing=args.allow_missing,
        )
    emit_lookup_telemetry(
        args.telemetry_file,
        args.run_id,
        payload,
        tier_requested=args.tier if not (args.file_path or args.symbol_name) else None,
        tier_returned=args.tier if not (args.file_path or args.symbol_name) else None,
        file_path=args.file_path,
        symbol_name=args.symbol_name,
    )
    json.dump(payload, sys.stdout, indent=2)
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
