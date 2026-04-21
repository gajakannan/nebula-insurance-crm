#!/usr/bin/env python3
from __future__ import annotations

import fnmatch
import json
import math
import os
import re
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any, Dict, Iterable, List, Mapping, Tuple

try:
    import yaml
except ImportError as exc:  # pragma: no cover - import guard
    raise SystemExit(
        "PyYAML is required for scripts/kg tooling. Install it with `pip install pyyaml`."
    ) from exc


REPO_ROOT = Path(__file__).resolve().parents[2]
KG_DIR = REPO_ROOT / "planning-mds" / "knowledge-graph"
FEATURES_DIR = REPO_ROOT / "planning-mds" / "features"
WILDCARD_RE = re.compile(r"[*?\[]")
FEATURE_ID_RE = re.compile(r"^feature:F\d{4}$")
STORY_ID_RE = re.compile(r"^story:F\d{4}-S\d{4}$")
BARE_FEATURE_ID_RE = re.compile(r"^F\d{4}$")
BARE_STORY_ID_RE = re.compile(r"^F\d{4}-S\d{4}$")

SECTION_TYPES = {
    "entities": "entity",
    "glossary_terms": "glossary_term",
    "workflows": "workflow",
    "capabilities": "capability",
    "endpoints": "endpoint",
    "ui_routes": "ui_route",
    "events": "event",
    "config_keys": "config_key",
    "migrations": "migration",
    "roles": "role",
    "policy_rules": "policy_rule",
    "evidence": "evidence",
    "adrs": "adr",
    "schemas": "schema",
    "api_contracts": "api_contract",
}

REF_FIELDS = (
    "affects",
    "governed_by",
    "uses_schema",
    "uses_api_contract",
    "depends_on",
    "restricted_to_role",
    "enforced_by_policy",
    "workflow_states",
    "validated_by",
    "supersedes",
)

VALID_PROVENANCE = {"extracted", "inferred", "ambiguous"}
TELEMETRY_ENV_VARS = {
    "action": "NEBULA_ACTION",
    "feature_id": "NEBULA_FEATURE_ID",
    "mode": "NEBULA_MODE",
    "gate": "NEBULA_GATE",
    "topic": "NEBULA_TOPIC",
}


def edge_ref_id(ref: str | dict[str, Any]) -> str:
    """Extract the node ID from an edge reference (bare string or object)."""
    if isinstance(ref, str):
        return ref
    return ref["id"]


def edge_ref_provenance(ref: str | dict[str, Any]) -> dict[str, Any] | None:
    """Extract provenance info from an edge reference, or None if bare string."""
    if isinstance(ref, str):
        return None
    prov = ref.get("provenance")
    if prov is None:
        return None
    result: dict[str, Any] = {"provenance": prov}
    if prov == "inferred":
        result["confidence"] = ref.get("confidence", 0.5)
    return result


def edge_ref_ids(refs: list[Any]) -> list[str]:
    """Extract node IDs from a list of edge references (bare strings or objects)."""
    return [edge_ref_id(r) for r in refs]


def load_yaml(path: Path) -> dict[str, Any]:
    try:
        data = yaml.safe_load(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise SystemExit(f"Missing required file: {repo_relative(path)}") from exc
    except yaml.YAMLError as exc:
        raise SystemExit(f"Failed to parse YAML: {repo_relative(path)}: {exc}") from exc

    return data or {}


def repo_relative(path: Path | str) -> str:
    resolved = Path(path)
    if resolved.is_absolute():
        resolved = resolved.resolve()
        try:
            resolved = resolved.relative_to(REPO_ROOT)
        except ValueError:
            return resolved.as_posix()
    return resolved.as_posix()


def normalize_repo_path(path: str) -> str:
    candidate = Path(path)
    if candidate.is_absolute():
        candidate = candidate.resolve()
        try:
            candidate = candidate.relative_to(REPO_ROOT)
        except ValueError:
            return candidate.as_posix()
    return candidate.as_posix()


def has_wildcards(pattern: str) -> bool:
    return bool(WILDCARD_RE.search(pattern))


def expand_declared_pattern(pattern: str) -> list[str]:
    normalized = normalize_repo_path(pattern)
    if has_wildcards(normalized):
        return sorted(
            repo_relative(path)
            for path in REPO_ROOT.glob(normalized)
            if path.exists()
        )

    candidate = REPO_ROOT / normalized
    return [normalized] if candidate.exists() else []


def normalize_target_id(target: str) -> str:
    stripped = target.strip()
    if BARE_FEATURE_ID_RE.fullmatch(stripped):
        return f"feature:{stripped}"
    if BARE_STORY_ID_RE.fullmatch(stripped):
        return f"story:{stripped}"
    return stripped


def build_bundle(
    ontology: Mapping[str, Any],
    canonical: Mapping[str, Any],
    mappings: Mapping[str, Any],
    code_index: Mapping[str, Any],
) -> dict[str, Any]:
    canonical_nodes = flatten_canonical_nodes(canonical)
    mapping_nodes = flatten_mapping_nodes(mappings)
    all_nodes = {**canonical_nodes, **mapping_nodes}
    bindings = build_binding_index(code_index)

    return {
        "ontology": dict(ontology),
        "canonical": dict(canonical),
        "mappings": dict(mappings),
        "code_index": dict(code_index),
        "canonical_nodes": canonical_nodes,
        "mapping_nodes": mapping_nodes,
        "all_nodes": all_nodes,
        "bindings": bindings,
    }


def load_bundle() -> dict[str, Any]:
    ontology = load_yaml(KG_DIR / "solution-ontology.yaml")
    canonical = load_yaml(KG_DIR / "canonical-nodes.yaml")
    mappings = load_yaml(KG_DIR / "feature-mappings.yaml")
    code_index = load_yaml(KG_DIR / "code-index.yaml")
    return build_bundle(ontology, canonical, mappings, code_index)


def flatten_canonical_nodes(canonical: Mapping[str, Any]) -> dict[str, dict[str, Any]]:
    nodes: dict[str, dict[str, Any]] = {}

    for section, node_type in SECTION_TYPES.items():
        for item in canonical.get(section, []):
            node = dict(item)
            node["_kind"] = node_type
            nodes[node["id"]] = node

            if section == "workflows":
                for state in item.get("states", []):
                    state_node = dict(state)
                    state_node["_kind"] = "workflow_state"
                    state_node["belongs_to_workflow"] = item["id"]
                    nodes[state_node["id"]] = state_node

    return nodes


def flatten_mapping_nodes(mappings: Mapping[str, Any]) -> dict[str, dict[str, Any]]:
    nodes: dict[str, dict[str, Any]] = {}

    for item in mappings.get("features", []):
        node = dict(item)
        node["_kind"] = "feature"
        nodes[node["id"]] = node

    for item in mappings.get("coverage", {}).get("excluded_features", []):
        node = dict(item)
        node["_kind"] = "feature"
        node["excluded"] = True
        nodes[node["id"]] = node

    for item in mappings.get("stories", []):
        node = dict(item)
        node["_kind"] = "story"
        nodes[node["id"]] = node

    return nodes


def _collect_patterns(value: Any, labels: list[str] | None = None) -> list[dict[str, str]]:
    labels = labels or []
    collected: list[dict[str, str]] = []

    if isinstance(value, str):
        collected.append(
            {
                "bucket": ".".join(labels) if labels else "paths",
                "pattern": normalize_repo_path(value),
            }
        )
        return collected

    if isinstance(value, list):
        for item in value:
            collected.extend(_collect_patterns(item, labels))
        return collected

    if isinstance(value, dict):
        for key, child in value.items():
            collected.extend(_collect_patterns(child, [*labels, key]))
        return collected

    return collected


def build_binding_index(code_index: Mapping[str, Any]) -> dict[str, dict[str, Any]]:
    bindings: dict[str, dict[str, Any]] = {}

    for entry in code_index.get("node_bindings", []):
        binding = dict(entry)
        binding["declared_paths"] = _collect_patterns(binding.get("paths", {}))
        bindings[binding["id"]] = binding

    return bindings


def resolve_node(node_id: str, bundle: Mapping[str, Any]) -> dict[str, Any] | None:
    node = bundle["all_nodes"].get(node_id)
    if node is None:
        return None

    resolved = dict(node)
    binding = bundle["bindings"].get(node_id)
    if binding:
        resolved["code_paths"] = binding.get("paths", {})
    return resolved


def resolve_refs(ref_ids: Iterable[str], bundle: Mapping[str, Any]) -> list[dict[str, Any]]:
    resolved: list[dict[str, Any]] = []
    for ref_id in ref_ids:
        node = resolve_node(ref_id, bundle)
        if node is not None:
            resolved.append(node)
    return resolved


def iter_feature_dirs() -> list[str]:
    feature_dirs: list[str] = []
    for path in sorted(FEATURES_DIR.glob("*")):
        if path.is_dir() and path.name != "archive":
            feature_dirs.append(repo_relative(path))

    archive_dir = FEATURES_DIR / "archive"
    if archive_dir.exists():
        for path in sorted(archive_dir.glob("*")):
            if path.is_dir():
                feature_dirs.append(repo_relative(path))

    return feature_dirs


def excluded_feature_paths(mappings: Mapping[str, Any]) -> set[str]:
    coverage = mappings.get("coverage", {})
    return {
        normalize_repo_path(item["path"])
        for item in coverage.get("excluded_features", [])
        if item.get("path")
    }


def match_bindings_for_path(path: str, bundle: Mapping[str, Any]) -> list[dict[str, Any]]:
    normalized = normalize_repo_path(path)
    matches: list[dict[str, Any]] = []

    for binding in bundle["bindings"].values():
        matched = [
            entry
            for entry in binding.get("declared_paths", [])
            if fnmatch.fnmatch(normalized, entry["pattern"])
        ]
        if matched:
            entry = dict(binding)
            entry["matched_patterns"] = matched
            matches.append(entry)

    return sorted(matches, key=lambda item: item["id"])


def related_mapping_entries(
    node_ids: Iterable[str],
    mappings: Mapping[str, Any],
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    wanted = set(node_ids)
    features: list[dict[str, Any]] = []
    stories: list[dict[str, Any]] = []

    for item in mappings.get("features", []):
        refs = set()
        for field in REF_FIELDS:
            refs.update(edge_ref_ids(item.get(field, [])))
        if refs.intersection(wanted):
            features.append(dict(item))

    for item in mappings.get("stories", []):
        refs = {item.get("feature")} if item.get("feature") else set()
        for field in REF_FIELDS:
            refs.update(edge_ref_ids(item.get(field, [])))
        if refs.intersection(wanted):
            stories.append(dict(item))

    features.sort(key=lambda item: item["id"])
    stories.sort(key=lambda item: item["id"])
    return features, stories


def planning_scope_for_path(path: str, mappings: Mapping[str, Any]) -> dict[str, Any]:
    normalized = normalize_repo_path(path)

    for story in mappings.get("stories", []):
        if normalize_repo_path(story.get("path", "")) == normalized:
            return {"story": dict(story)}

    for feature in mappings.get("features", []):
        feature_path = normalize_repo_path(feature.get("path", ""))
        if normalized == feature_path or normalized.startswith(f"{feature_path}/"):
            return {"feature": dict(feature)}

    return {}


def feature_or_story_by_id(target_id: str, mappings: Mapping[str, Any]) -> dict[str, Any] | None:
    if FEATURE_ID_RE.fullmatch(target_id):
        for item in mappings.get("features", []):
            if item["id"] == target_id:
                return dict(item)
    if STORY_ID_RE.fullmatch(target_id):
        for item in mappings.get("stories", []):
            if item["id"] == target_id:
                return dict(item)
    return None


def id_patterns_by_type(ontology: Mapping[str, Any]) -> dict[str, str]:
    return {
        item["type"]: item["pattern"]
        for item in ontology.get("id_patterns", [])
        if item.get("type") and item.get("pattern")
    }


def type_regex_map() -> dict[str, re.Pattern[str]]:
    slug = r"[a-z0-9]+(?:-[a-z0-9]+)*"
    return {
        "entity": re.compile(rf"^entity:{slug}$"),
        "workflow": re.compile(rf"^workflow:{slug}$"),
        "workflow_state": re.compile(rf"^state:{slug}:{slug}$"),
        "schema": re.compile(rf"^schema:{slug}$"),
        "capability": re.compile(rf"^capability:{slug}$"),
        "role": re.compile(rf"^role:{slug}$"),
        "policy_rule": re.compile(rf"^policy_rule:{slug}$"),
        "api_contract": re.compile(rf"^api:{slug}$"),
        "adr": re.compile(rf"^adr:[a-z0-9]+(?:-[a-z0-9]+)*$"),
        "feature": re.compile(r"^feature:F\d{4}$"),
        "story": re.compile(r"^story:F\d{4}-S\d{4}$"),
    }


def now_iso() -> str:
    return datetime.now(UTC).isoformat(timespec="seconds")


def telemetry_context_from_env() -> dict[str, str | None]:
    """Read the shared Nebula telemetry context from environment variables.

    Supported environment variables:
    - NEBULA_ACTION
    - NEBULA_FEATURE_ID
    - NEBULA_MODE
    - NEBULA_GATE
    - NEBULA_TOPIC
    """
    return {
        field: os.getenv(env_name) or None
        for field, env_name in TELEMETRY_ENV_VARS.items()
    }


def estimate_tokens(value: Any) -> int:
    """Best-effort token estimate for telemetry budgeting.

    This intentionally stays lightweight and deterministic so CLIs can emit
    comparable telemetry without depending on a tokenizer package.
    """
    serialized = json.dumps(value, sort_keys=True, ensure_ascii=False, default=str)
    return max(1, math.ceil(len(serialized) / 4))


def emit_telemetry(
    telemetry_file: Path | None,
    run_id: str | None,
    tool: str,
    event: dict[str, Any],
) -> None:
    """Append a single JSONL telemetry event.

    The payload is enriched with the shared action context from environment
    variables when present. If telemetry_file is None, this is a no-op.
    """
    if telemetry_file is None:
        return

    payload = {
        "ts": now_iso(),
        "run_id": run_id,
        "tool": tool,
        **telemetry_context_from_env(),
        "payload": event,
    }

    telemetry_file.parent.mkdir(parents=True, exist_ok=True)
    with telemetry_file.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(payload, ensure_ascii=False))
        handle.write("\n")
        handle.flush()


def main_exception(message: str) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(1)
