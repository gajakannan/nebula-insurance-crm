#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import re
from datetime import UTC, datetime
from pathlib import Path
from typing import Any, Iterable

import yaml

from kg_common import (
    KG_DIR,
    REF_FIELDS,
    REPO_ROOT,
    SECTION_TYPES,
    VALID_PROVENANCE,
    edge_ref_id,
    edge_ref_ids,
    edge_ref_provenance,
    excluded_feature_paths,
    expand_declared_pattern,
    iter_feature_dirs,
    load_bundle,
    normalize_repo_path,
    type_regex_map,
)


class ValidationReport:
    def __init__(self) -> None:
        self.errors: list[str] = []
        self.warnings: list[str] = []

    def error(self, message: str) -> None:
        self.errors.append(message)

    def warn(self, message: str) -> None:
        self.warnings.append(message)


COVERAGE_REPORT_PATH = KG_DIR / "coverage-report.yaml"


def validate_id(report: ValidationReport, node_id: str, node_type: str, patterns: dict[str, Any]) -> None:
    regex = patterns.get(node_type)
    if regex is None:
        return
    if not regex.fullmatch(node_id):
        report.error(f"ID does not match {node_type} pattern: {node_id}")


def validate_path_exists(report: ValidationReport, path_value: str, context: str) -> None:
    normalized = normalize_repo_path(path_value)
    candidate = Path(normalized)
    if not (REPO_ROOT / candidate).exists():
        report.error(f"Missing path for {context}: {normalized}")


def validate_references(report: ValidationReport, item: dict[str, Any], all_nodes: dict[str, Any]) -> None:
    item_id = item["id"]
    for field in REF_FIELDS:
        for ref in item.get(field, []):
            ref_id = edge_ref_id(ref)
            if ref_id not in all_nodes:
                report.error(f"Unknown reference in {item_id}.{field}: {ref_id}")
            validate_edge_provenance(report, ref, f"{item_id}.{field}")

    feature_ref = item.get("feature")
    if feature_ref and feature_ref not in all_nodes:
        report.error(f"Unknown feature reference in {item_id}.feature: {feature_ref}")


def validate_edge_provenance(report: ValidationReport, ref: Any, context: str) -> None:
    """Validate provenance annotation on an edge reference."""
    if isinstance(ref, str):
        return
    if not isinstance(ref, dict):
        report.error(f"Edge reference in {context} is neither string nor object: {ref!r}")
        return

    ref_id = ref.get("id")
    if not ref_id:
        report.error(f"Edge reference object in {context} is missing 'id'")
        return

    prov = ref.get("provenance")
    if prov is None:
        return

    if prov not in VALID_PROVENANCE:
        report.error(f"Invalid provenance '{prov}' on {ref_id} in {context} (valid: {', '.join(sorted(VALID_PROVENANCE))})")
        return

    if prov == "inferred":
        confidence = ref.get("confidence")
        if confidence is None:
            report.warn(f"Inferred edge {ref_id} in {context} is missing confidence score")
        elif not isinstance(confidence, (int, float)) or confidence < 0.0 or confidence > 1.0:
            report.error(f"Invalid confidence {confidence!r} on {ref_id} in {context} (must be 0.0–1.0)")
        elif confidence < 0.5:
            report.warn(f"Low-confidence inferred edge ({confidence}) on {ref_id} in {context}")

    if prov == "ambiguous":
        report.warn(f"Ambiguous edge {ref_id} in {context} — flagged for architect review")


def validate_rationale_entry(
    report: ValidationReport, entry: Any, node_id: str, all_nodes: dict[str, Any]
) -> None:
    """Validate a single rationale entry on a canonical node."""
    if not isinstance(entry, dict):
        report.error(f"Rationale entry on {node_id} is not an object: {entry!r}")
        return

    adr_ref = entry.get("adr")
    if not adr_ref:
        report.error(f"Rationale entry on {node_id} is missing 'adr' field")
        return
    if adr_ref not in all_nodes:
        report.error(f"Rationale on {node_id} references unknown ADR: {adr_ref}")

    if not entry.get("section"):
        report.error(f"Rationale entry on {node_id} (adr: {adr_ref}) is missing 'section' field")

    if not entry.get("summary"):
        report.error(f"Rationale entry on {node_id} (adr: {adr_ref}) is missing 'summary' field")


def iter_existing_files(paths: Iterable[str]) -> list[Path]:
    files: list[Path] = []
    seen: set[str] = set()

    for value in paths:
        normalized = normalize_repo_path(value)
        candidate = REPO_ROOT / normalized
        if not candidate.exists():
            continue

        if candidate.is_dir():
            nested = sorted(path for path in candidate.rglob("*") if path.is_file())
            for path in nested:
                rel = path.relative_to(REPO_ROOT).as_posix()
                if rel not in seen:
                    seen.add(rel)
                    files.append(path)
            continue

        rel = candidate.relative_to(REPO_ROOT).as_posix()
        if rel not in seen:
            seen.add(rel)
            files.append(candidate)

    return files


def digest_files(paths: Iterable[Path]) -> str:
    hasher = hashlib.sha256()
    for path in sorted(paths):
        rel = path.relative_to(REPO_ROOT).as_posix()
        hasher.update(rel.encode("utf-8"))
        hasher.update(b"\0")
        hasher.update(path.read_bytes())
        hasher.update(b"\0")
    return hasher.hexdigest()[:16]


def latest_modified_date(paths: Iterable[Path]) -> str | None:
    timestamps = [path.stat().st_mtime for path in paths]
    if not timestamps:
        return None
    return datetime.fromtimestamp(max(timestamps), UTC).date().isoformat()


def build_freshness_entry(source_paths: Iterable[str]) -> dict[str, Any]:
    files = iter_existing_files(source_paths)
    relative_files = [path.relative_to(REPO_ROOT).as_posix() for path in files]
    return {
        "source_paths": relative_files,
        "source_count": len(relative_files),
        "last_modified": latest_modified_date(files),
        "source_hash": digest_files(files) if files else None,
    }


def build_coverage_report(
    bundle: dict[str, Any],
    mapped_feature_paths: set[str],
    excluded_paths: set[str],
    uncovered: list[str],
) -> dict[str, Any]:
    canonical = bundle["canonical"]
    mappings = bundle["mappings"]
    code_index = bundle["code_index"]

    canonical_freshness: dict[str, Any] = {}
    for section in SECTION_TYPES:
        for item in canonical.get(section, []):
            source_paths = list(item.get("source_docs", []))
            if item.get("path"):
                source_paths.append(item["path"])
            canonical_freshness[item["id"]] = build_freshness_entry(source_paths)

    mapping_freshness: dict[str, Any] = {}
    for section_name in ("features", "stories"):
        for item in mappings.get(section_name, []):
            mapping_freshness[item["id"]] = build_freshness_entry([item["path"]])

    binding_freshness: dict[str, Any] = {}
    for binding in code_index.get("node_bindings", []):
        declared_paths = bundle["bindings"].get(binding["id"], {}).get("declared_paths", [])
        resolved: list[str] = []
        for entry in declared_paths:
            resolved.extend(expand_declared_pattern(entry["pattern"]))
        binding_freshness[binding["id"]] = build_freshness_entry(resolved)

    return {
        "version": 0,
        "summary": {
            "canonical_nodes": len(bundle["canonical_nodes"]),
            "features_mapped": len(mappings.get("features", [])),
            "stories_mapped": len(mappings.get("stories", [])),
            "features_excluded": len(excluded_paths),
            "features_uncovered": len(uncovered),
            "code_bindings": len(code_index.get("node_bindings", [])),
        },
        "coverage": {
            "mapped_feature_ids": [item["id"] for item in mappings.get("features", [])],
            "excluded_features": mappings.get("coverage", {}).get("excluded_features", []),
            "uncovered_feature_paths": uncovered,
            "mapped_story_ids": [item["id"] for item in mappings.get("stories", [])],
        },
        "freshness": {
            "canonical": canonical_freshness,
            "mappings": mapping_freshness,
            "code_bindings": binding_freshness,
        },
    }


def write_coverage_report(report_payload: dict[str, Any]) -> None:
    COVERAGE_REPORT_PATH.write_text(
        yaml.safe_dump(report_payload, sort_keys=False, allow_unicode=False),
        encoding="utf-8",
    )


# ---------------------------------------------------------------------------
# Drift checkers
# ---------------------------------------------------------------------------

MEMORY_LINK_RE = re.compile(r"\[([^\]]+)\]\(\./?([\w.-]+\.md)\)")
REPO_PATH_RE = re.compile(
    r"`((?:agents|planning-mds|engine|experience|neuron|scripts|docker"
    r"|\.github)/[\w./*\-]+)`"
)


def validate_external_memory_drift(report: ValidationReport, memory_dir: Path) -> None:
    """Check an external agent memory directory for stale repo-path references.

    Agent-agnostic: any coding agent that stores file-based memory can pass its
    memory directory via --memory-dir. No vendor-specific path conventions are
    assumed.

    Checks performed:
    1. If an index file (MEMORY.md or similar) links to .md files, verify they exist.
    2. All .md files in the directory are scanned for backtick-quoted repo paths
       that no longer resolve — a signal the memory is stale.
    """
    if not memory_dir.is_dir():
        report.error(f"--memory-dir path is not a directory: {memory_dir}")
        return

    # Optional index file — check linked files if present
    memory_md = memory_dir / "MEMORY.md"
    if memory_md.exists():
        content = memory_md.read_text(encoding="utf-8")
        linked_files = {m.group(2): m.group(1) for m in MEMORY_LINK_RE.finditer(content)}

        for filename in linked_files:
            if not (memory_dir / filename).exists():
                report.error(f"Memory index links to missing file: {filename}")

        for path in sorted(memory_dir.glob("*.md")):
            if path.name == "MEMORY.md":
                continue
            if path.name not in linked_files:
                report.warn(f"Memory file not indexed: {path.name}")

    # Scan all .md files for dead repo-path references
    for path in sorted(memory_dir.glob("*.md")):
        file_content = path.read_text(encoding="utf-8")
        for match in REPO_PATH_RE.finditer(file_content):
            ref_path = match.group(1)
            if "*" in ref_path:
                continue
            if not (REPO_ROOT / ref_path).exists():
                report.warn(
                    f"Memory file {path.name} references missing repo path: {ref_path}"
                )


def parse_casbin_policy_pairs(policy_path: Path) -> set[tuple[str, str]]:
    """Extract unique (resource, action) pairs from policy.csv."""
    pairs: set[tuple[str, str]] = set()
    for line in policy_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        parts = [p.strip() for p in line.split(",")]
        if len(parts) >= 4 and parts[0] == "p":
            pairs.add((parts[2], parts[3]))
    return pairs


def parse_casbin_role_map(
    policy_path: Path,
) -> dict[tuple[str, str], set[str]]:
    """Map (resource, action) → set of roles from policy.csv."""
    role_map: dict[tuple[str, str], set[str]] = {}
    for line in policy_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        parts = [p.strip() for p in line.split(",")]
        if len(parts) >= 4 and parts[0] == "p":
            key = (parts[2], parts[3])
            role_map.setdefault(key, set()).add(parts[1])
    return role_map


ROLE_SLUG_TO_CSV = {
    "distribution-user": "DistributionUser",
    "distribution-manager": "DistributionManager",
    "underwriter": "Underwriter",
    "relationship-manager": "RelationshipManager",
    "program-manager": "ProgramManager",
    "admin": "Admin",
    "broker-user": "BrokerUser",
    "coordinator": "Coordinator",
    "mga-user": "MgaUser",
    "external-user": "ExternalUser",
}


def validate_casbin_drift(
    report: ValidationReport, bundle: dict[str, Any]
) -> None:
    """Cross-check policy_rule nodes against actual policy.csv entries."""
    policy_path = REPO_ROOT / "planning-mds" / "security" / "policies" / "policy.csv"
    if not policy_path.exists():
        report.warn("policy.csv not found; skipping Casbin drift check")
        return

    actual_pairs = parse_casbin_policy_pairs(policy_path)
    actual_roles = parse_casbin_role_map(policy_path)
    canonical = bundle["canonical"]

    declared_pairs: set[tuple[str, str]] = set()
    declared_rules: dict[tuple[str, str], dict[str, Any]] = {}
    for rule in canonical.get("policy_rules", []):
        resource = rule.get("resource")
        action = rule.get("action")
        if resource and action:
            pair = (resource, action)
            declared_pairs.add(pair)
            declared_rules[pair] = rule

    # Pairs in policy.csv but not in canonical-nodes
    for resource, action in sorted(actual_pairs - declared_pairs):
        report.warn(
            f"Casbin policy pair ({resource}, {action}) in policy.csv "
            f"has no policy_rule node in canonical-nodes.yaml"
        )

    # Pairs in canonical-nodes but not in policy.csv
    for resource, action in sorted(declared_pairs - actual_pairs):
        report.error(
            f"policy_rule declares ({resource}, {action}) but no matching "
            f"lines exist in policy.csv"
        )

    # Role-level mismatch for shared pairs
    for pair in sorted(declared_pairs & actual_pairs):
        rule = declared_rules[pair]
        declared_role_slugs = {
            r.replace("role:", "") for r in rule.get("allowed_roles", [])
        }
        declared_csv_roles = {
            ROLE_SLUG_TO_CSV[s]
            for s in declared_role_slugs
            if s in ROLE_SLUG_TO_CSV
        }
        actual_csv_roles = actual_roles.get(pair, set())

        missing_in_ontology = actual_csv_roles - declared_csv_roles
        missing_in_csv = declared_csv_roles - actual_csv_roles

        resource, action = pair
        for role in sorted(missing_in_ontology):
            report.warn(
                f"policy.csv grants {role} ({resource}, {action}) but "
                f"policy_rule:{resource}-{action} omits it from allowed_roles"
            )
        for role in sorted(missing_in_csv):
            report.warn(
                f"policy_rule:{resource}-{action} declares {role} in "
                f"allowed_roles but no matching policy.csv line exists"
            )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate knowledge-graph integrity.")
    parser.add_argument(
        "--write-coverage-report",
        action="store_true",
        help="Write planning-mds/knowledge-graph/coverage-report.yaml using current KG state.",
    )
    parser.add_argument(
        "--check-drift",
        action="store_true",
        help="Run drift checks: Casbin policy cross-check, and external memory staleness (if --memory-dir given).",
    )
    parser.add_argument(
        "--memory-dir",
        type=Path,
        default=None,
        help="Path to an external agent memory directory to scan for stale repo-path references. Agent-agnostic — works with any tool that stores .md memory files.",
    )
    args = parser.parse_args()

    bundle = load_bundle()
    report = ValidationReport()
    regex_by_type = type_regex_map()

    ontology = bundle["ontology"]
    canonical = bundle["canonical"]
    mappings = bundle["mappings"]
    code_index = bundle["code_index"]
    all_nodes = bundle["all_nodes"]

    seen_ids: set[str] = set()

    for section, node_type in SECTION_TYPES.items():
        for item in canonical.get(section, []):
            node_id = item["id"]
            if node_id in seen_ids:
                report.error(f"Duplicate ID: {node_id}")
            seen_ids.add(node_id)
            validate_id(report, node_id, node_type, regex_by_type)

            if item.get("path"):
                validate_path_exists(report, item["path"], node_id)
            for source_doc in item.get("source_docs", []):
                validate_path_exists(report, source_doc, f"{node_id}.source_docs")
            for related_id in item.get("related_nodes", []):
                if related_id not in all_nodes:
                    report.error(f"Unknown related node in {node_id}.related_nodes: {related_id}")
            for role_id in item.get("allowed_roles", []):
                if role_id not in all_nodes:
                    report.error(f"Unknown role reference in {node_id}.allowed_roles: {role_id}")
            for rationale_entry in item.get("rationale", []):
                validate_rationale_entry(report, rationale_entry, node_id, all_nodes)

            if section == "workflows":
                workflow_id = item["id"]
                state_ids = {state["id"] for state in item.get("states", [])}
                for state in item.get("states", []):
                    state_id = state["id"]
                    if state_id in seen_ids:
                        report.error(f"Duplicate ID: {state_id}")
                    seen_ids.add(state_id)
                    validate_id(report, state_id, "workflow_state", regex_by_type)
                    for target_id in state.get("transitions_to", []):
                        if target_id not in state_ids:
                            report.error(
                                f"Workflow state transition leaves workflow in {workflow_id}: "
                                f"{state_id} -> {target_id}"
                            )

    for section_name in ("features", "stories"):
        node_type = "feature" if section_name == "features" else "story"
        for item in mappings.get(section_name, []):
            node_id = item["id"]
            if node_id in seen_ids:
                report.error(f"Duplicate ID: {node_id}")
            seen_ids.add(node_id)
            validate_id(report, node_id, node_type, regex_by_type)
            validate_path_exists(report, item["path"], node_id)
            validate_references(report, item, all_nodes)

    mapped_feature_paths = {
        normalize_repo_path(item["path"])
        for item in mappings.get("features", [])
    }
    excluded_paths = excluded_feature_paths(mappings)
    feature_dirs = set(iter_feature_dirs())
    uncovered = sorted(feature_dirs - mapped_feature_paths - excluded_paths)
    for path in uncovered:
        report.error(f"Feature directory is neither mapped nor excluded: {path}")

    excluded_ids: set[str] = set()
    for item in mappings.get("coverage", {}).get("excluded_features", []):
        feature_id = item.get("id")
        path = item.get("path")
        reason = item.get("reason")
        if not feature_id or not path or not reason:
            report.error("Each coverage.excluded_features entry requires id, path, and reason")
            continue
        if feature_id in excluded_ids:
            report.error(f"Duplicate excluded feature entry: {feature_id}")
        excluded_ids.add(feature_id)
        validate_id(report, feature_id, "feature", regex_by_type)
        validate_path_exists(report, path, f"coverage.excluded_features:{feature_id}")

    binding_ids: set[str] = set()
    for binding in code_index.get("node_bindings", []):
        node_id = binding.get("id")
        if not node_id:
            report.error("code-index node binding is missing id")
            continue
        if node_id in binding_ids:
            report.error(f"Duplicate code-index binding: {node_id}")
        binding_ids.add(node_id)
        if node_id not in all_nodes:
            report.error(f"code-index binding references unknown node: {node_id}")

        declared_paths = bundle["bindings"].get(node_id, {}).get("declared_paths", [])
        if not declared_paths:
            report.error(f"code-index binding has no paths: {node_id}")
            continue
        for entry in declared_paths:
            matches = expand_declared_pattern(entry["pattern"])
            if not matches:
                report.error(
                    f"code-index pattern does not resolve for {node_id} ({entry['bucket']}): "
                    f"{entry['pattern']}"
                )

    edge_usage = {
        "transitions_to": 0,
        "validated_by": 0,
        "supersedes": 0,
    }
    for workflow in canonical.get("workflows", []):
        for state in workflow.get("states", []):
            edge_usage["transitions_to"] += len(state.get("transitions_to", []))
    for section_name in ("features", "stories"):
        for item in mappings.get(section_name, []):
            edge_usage["validated_by"] += len(item.get("validated_by", []))
            edge_usage["supersedes"] += len(item.get("supersedes", []))

    for edge in ontology.get("edge_types", []):
        edge_id = edge["id"]
        if edge_id in edge_usage and edge_usage[edge_id] == 0:
            report.warn(f"Declared edge type is unused: {edge_id}")

    if args.check_drift:
        validate_casbin_drift(report, bundle)
        if args.memory_dir:
            validate_external_memory_drift(report, args.memory_dir)

    coverage_report = build_coverage_report(bundle, mapped_feature_paths, excluded_paths, uncovered)
    if args.write_coverage_report:
        write_coverage_report(coverage_report)
    else:
        if not COVERAGE_REPORT_PATH.exists():
            report.error(
                "Missing coverage report: planning-mds/knowledge-graph/coverage-report.yaml "
                "(run python3 scripts/kg/validate.py --write-coverage-report)"
            )
        else:
            existing = yaml.safe_load(COVERAGE_REPORT_PATH.read_text(encoding="utf-8")) or {}

            def _extract_hashes(rpt: dict[str, Any]) -> dict[str, str | None]:
                """Extract only source_hash values for staleness comparison.

                last_modified uses st_mtime which differs between local and CI
                (git checkout sets all timestamps to checkout time), so we only
                compare content hashes which are deterministic."""
                hashes: dict[str, str | None] = {}
                for section in ("canonical", "mappings", "code_bindings"):
                    for key, entry in rpt.get("freshness", {}).get(section, {}).items():
                        hashes[f"{section}/{key}"] = entry.get("source_hash")
                return hashes

            if _extract_hashes(existing) != _extract_hashes(coverage_report):
                report.error(
                    "coverage-report.yaml is stale "
                    "(run python3 scripts/kg/validate.py --write-coverage-report)"
                )

    print("Knowledge graph validation")
    print("-" * 60)
    print(f"Features mapped:   {len(mappings.get('features', []))}")
    print(f"Stories mapped:    {len(mappings.get('stories', []))}")
    print(
        "Feature coverage:  "
        f"{len(mapped_feature_paths)} mapped, {len(excluded_paths)} excluded, {len(uncovered)} uncovered"
    )
    print(f"Code bindings:     {len(code_index.get('node_bindings', []))}")

    if report.warnings:
        print("\nWarnings:")
        for warning in report.warnings:
            print(f"- {warning}")

    if report.errors:
        print("\nErrors:")
        for error in report.errors:
            print(f"- {error}")
        return 1

    print("\n[PASS] knowledge-graph integrity checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
