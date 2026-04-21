#!/usr/bin/env python3
"""
BrokerUser Policy Parity Gate (F-007)

Verifies that authorization-matrix.md §2.10 (BrokerUser ALLOW decisions)
and policy.csv §2.10 (BrokerUser Casbin rows) remain in sync.

Exit codes:
  0 — parity confirmed
  1 — parity failure (mismatches reported to stdout)

Usage:
  python3 scripts/check-policy-parity.py
  python3 scripts/check-policy-parity.py --matrix planning-mds/security/authorization-matrix.md
                                          --policy  planning-mds/security/policies/policy.csv
"""

import argparse
import re
import sys
from pathlib import Path
from typing import Set, Tuple

# ---------------------------------------------------------------------------
# Defaults (relative to repo root — where the script is expected to be run
# from when invoked as  python3 scripts/check-policy-parity.py)
# ---------------------------------------------------------------------------
DEFAULT_MATRIX = "planning-mds/security/authorization-matrix.md"
DEFAULT_POLICY = "planning-mds/security/policies/policy.csv"

PolicyTuple = Tuple[str, str]  # (resource, action)


# ---------------------------------------------------------------------------
# Parser: policy.csv
# ---------------------------------------------------------------------------

def parse_policy_csv(policy_path: Path) -> Set[PolicyTuple]:
    """
    Extract all Casbin policy rows where role == "BrokerUser".

    Expected line format (with variable whitespace around commas):
      p, BrokerUser, <resource>, <action>, <condition>

    Comment lines (# ...) and blank lines are ignored.
    Returns a set of (resource, action) tuples.
    """
    rows: Set[PolicyTuple] = set()

    content = policy_path.read_text(encoding="utf-8", errors="ignore")
    for lineno, raw_line in enumerate(content.splitlines(), start=1):
        line = raw_line.strip()

        # Skip blank lines and comments
        if not line or line.startswith("#"):
            continue

        # Split on comma, stripping whitespace from each field
        parts = [p.strip() for p in line.split(",")]

        # Must have at least 5 fields: p, role, resource, action, condition
        if len(parts) < 5:
            continue

        policy_type, role, resource, action = parts[0], parts[1], parts[2], parts[3]

        # Only process "p" (permission) rows for BrokerUser
        if policy_type != "p" or role != "BrokerUser":
            continue

        rows.add((resource, action))

    return rows


# ---------------------------------------------------------------------------
# Parser: authorization-matrix.md §2.10
# ---------------------------------------------------------------------------

_SECTION_210_HEADER = re.compile(r"^#{1,4}\s+2\.10\b", re.IGNORECASE)
_NEXT_SECTION_HEADER = re.compile(r"^#{1,4}\s+\d+\.", re.IGNORECASE)

# Matches a markdown table row that starts with an optional pipe and "BrokerUser"
# E.g.:  | BrokerUser | broker | read / search | **ALLOW** | ...
_TABLE_ROW = re.compile(r"^\|?\s*([^|]+?)\s*\|")


def _split_action_cell(raw_action: str) -> list[str]:
    """
    Convert an action cell value into individual action tokens.

    Handles:
      "read"              → ["read"]
      "read / search"     → ["read", "search"]
      "create / update / delete / reactivate"  → ["create", "update", "delete", "reactivate"]
    """
    # Remove markdown bold markers and extra spaces
    cleaned = re.sub(r"\*\*", "", raw_action).strip()
    # Split on "/" (with optional surrounding whitespace)
    return [part.strip() for part in re.split(r"\s*/\s*", cleaned) if part.strip()]


def parse_matrix_md(matrix_path: Path) -> Set[PolicyTuple]:
    """
    Parse authorization-matrix.md and extract all BrokerUser ALLOW decisions
    from the §2.10 table.

    Table columns (§2.10): Role | Resource | Action | Decision | ...

    "read / search" style cells are split into separate tuples:
      ("broker", "read") and ("broker", "search")

    Returns a set of (resource, action) tuples for all ALLOW rows.
    """
    content = matrix_path.read_text(encoding="utf-8", errors="ignore")
    lines = content.splitlines()

    # ---- Step 1: locate §2.10 ----
    section_start = None
    for idx, line in enumerate(lines):
        if _SECTION_210_HEADER.match(line.strip()):
            section_start = idx
            break

    if section_start is None:
        raise ValueError(
            f"Could not find §2.10 section in {matrix_path}. "
            "Expected a heading matching '## 2.10' (or ### / ####)."
        )

    # ---- Step 2: extract lines belonging to §2.10 ----
    section_lines = []
    for idx in range(section_start + 1, len(lines)):
        line = lines[idx]
        # Stop at the next numbered section heading (e.g. "## 3.", "### 2.11")
        if _NEXT_SECTION_HEADER.match(line.strip()):
            break
        section_lines.append(line)

    # ---- Step 3: parse the markdown table rows ----
    allows: Set[PolicyTuple] = set()

    for line in section_lines:
        stripped = line.strip()

        # Must be a table row (contains pipe characters)
        if "|" not in stripped:
            continue

        # Split into cells, drop empty outer fields from leading/trailing pipes
        cells = [c.strip() for c in stripped.split("|")]
        # Remove empty strings that result from leading/trailing pipes
        cells = [c for c in cells if c]

        # §2.10 table has columns: Role | Resource | Action | Decision | ...
        # We need at least 4 cells
        if len(cells) < 4:
            continue

        role_cell, resource_cell, action_cell, decision_cell = (
            cells[0],
            cells[1],
            cells[2],
            cells[3],
        )

        # Skip header and separator rows
        if (
            role_cell.lower() in ("role", "---", ":---", "---:") or
            re.match(r"^[-:]+$", role_cell)
        ):
            continue

        # Only process BrokerUser rows
        if role_cell != "BrokerUser":
            continue

        # Only process ALLOW decisions
        decision_clean = re.sub(r"\*\*", "", decision_cell).strip()
        if decision_clean.upper() != "ALLOW":
            continue

        # Expand composite action cells ("read / search" → ["read", "search"])
        actions = _split_action_cell(action_cell)

        # resource_cell may also be composite in edge cases; treat as single token
        resource = resource_cell.strip()

        for action in actions:
            allows.add((resource, action))

    return allows


# ---------------------------------------------------------------------------
# Parity comparison
# ---------------------------------------------------------------------------

def compare(
    matrix_allows: Set[PolicyTuple],
    policy_rows: Set[PolicyTuple],
) -> list[str]:
    """
    Compare the two sets and return a list of mismatch messages.
    Empty list means parity is confirmed.
    """
    mismatches: list[str] = []

    in_matrix_not_policy = matrix_allows - policy_rows
    in_policy_not_matrix = policy_rows - matrix_allows

    for resource, action in sorted(in_matrix_not_policy):
        mismatches.append(
            f"  [MATRIX -> POLICY gap]  "
            f"matrix has ALLOW for ({resource!r}, {action!r}) "
            f"but no matching row found in policy.csv"
        )

    for resource, action in sorted(in_policy_not_matrix):
        mismatches.append(
            f"  [POLICY -> MATRIX gap]  "
            f"policy.csv has BrokerUser row for ({resource!r}, {action!r}) "
            f"but no corresponding ALLOW found in authorization-matrix.md §2.10"
        )

    return mismatches


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        description="BrokerUser policy parity gate (F-007): "
                    "checks authorization-matrix.md §2.10 vs policy.csv"
    )
    parser.add_argument(
        "--matrix",
        default=DEFAULT_MATRIX,
        help=f"Path to authorization-matrix.md (default: {DEFAULT_MATRIX})",
    )
    parser.add_argument(
        "--policy",
        default=DEFAULT_POLICY,
        help=f"Path to policy.csv (default: {DEFAULT_POLICY})",
    )
    args = parser.parse_args()

    matrix_path = Path(args.matrix)
    policy_path = Path(args.policy)

    # ---- Validate file existence ----
    missing = []
    if not matrix_path.exists():
        missing.append(f"authorization-matrix.md not found: {matrix_path}")
    if not policy_path.exists():
        missing.append(f"policy.csv not found: {policy_path}")
    if missing:
        for msg in missing:
            print(f"ERROR: {msg}")
        return 1

    # ---- Parse ----
    try:
        matrix_allows = parse_matrix_md(matrix_path)
    except ValueError as exc:
        print(f"ERROR parsing authorization-matrix.md: {exc}")
        return 1

    try:
        policy_rows = parse_policy_csv(policy_path)
    except Exception as exc:  # noqa: BLE001
        print(f"ERROR parsing policy.csv: {exc}")
        return 1

    # ---- Compare ----
    mismatches = compare(matrix_allows, policy_rows)

    if mismatches:
        print(
            f"PARITY FAILURE: BrokerUser policy parity check failed.\n"
            f"  {len(matrix_allows)} ALLOW decision(s) in authorization-matrix.md §2.10\n"
            f"  {len(policy_rows)} BrokerUser row(s) in policy.csv\n"
            f"\nMismatches ({len(mismatches)}):"
        )
        for mismatch in mismatches:
            print(mismatch)
        print(
            "\nFix: update policy.csv or authorization-matrix.md §2.10 "
            "so both files reflect the same BrokerUser ALLOW decisions."
        )
        return 1

    print(
        f"BrokerUser policy parity confirmed. "
        f"{len(matrix_allows)} allow decisions, {len(policy_rows)} policy rows."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
