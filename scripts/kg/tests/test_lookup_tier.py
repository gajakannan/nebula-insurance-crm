from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "scripts" / "kg"))

import lookup  # noqa: E402
from kg_common import load_bundle  # noqa: E402


@pytest.fixture(scope="module")
def bundle() -> dict[str, object]:
    return load_bundle()


def _lookup_target(
    bundle: dict[str, object],
    *,
    tier: int,
    fields: str = "full",
    target: str = "F0006",
    allow_missing: bool = False,
) -> dict[str, object]:
    return lookup.lookup_by_target(
        target,
        bundle,
        tier=tier,
        fields=fields,
        allow_missing=allow_missing,
    )


def _find_node(entries: list[dict[str, object]], node_id: str) -> dict[str, object]:
    return next(entry for entry in entries if entry["id"] == node_id)


def test_tier_1_returns_ids_and_labels_only(bundle: dict[str, object]) -> None:
    payload = _lookup_target(bundle, tier=1)

    workflow = _find_node(payload["affects"], "workflow:submission")
    assert workflow == {
        "id": "workflow:submission",
        "label": "Submission lifecycle",
    }

    schema = _find_node(payload["uses_schema"], "schema:submission")
    assert schema == {"id": "schema:submission"}


def test_tier_2_adds_rationale_without_source_docs(bundle: dict[str, object]) -> None:
    payload = _lookup_target(bundle, tier=2)

    document = _find_node(payload["affects"], "entity:document")
    assert "rationale" in document
    assert "source_docs" not in document

    workflow = _find_node(payload["affects"], "workflow:submission")
    assert "rationale" in workflow
    assert "source_docs" not in workflow


def test_tier_3_adds_source_docs_without_reading_file_contents(bundle: dict[str, object]) -> None:
    payload = _lookup_target(bundle, tier=3)

    workflow = _find_node(payload["affects"], "workflow:submission")
    assert workflow["source_docs"] == [
        "planning-mds/features/archive/F0006-submission-intake-workflow/README.md",
        "planning-mds/architecture/feature-assembly-plan.md",
        "planning-mds/architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md",
        "planning-mds/api/nebula-api.yaml",
    ]
    assert all(isinstance(path, str) and "/" in path for path in workflow["source_docs"])
    assert "This folder holds" not in workflow["source_docs"][0]


def test_tier_4_matches_prechange_behavior(bundle: dict[str, object]) -> None:
    scope = lookup.feature_or_story_by_id("feature:F0006", bundle["mappings"])
    assert scope is not None
    scope["_kind"] = "feature"

    expected = lookup.build_scope_payload(scope, bundle)
    actual = _lookup_target(bundle, tier=4)
    assert actual == expected


def test_allow_missing_returns_unmapped_payload() -> None:
    result = subprocess.run(
        ["python3", "scripts/kg/lookup.py", "F9999", "--allow-missing"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )

    assert result.returncode == 0
    assert json.loads(result.stdout) == {
        "feature_id": "F9999",
        "scope": None,
        "reason": "unmapped",
        "hints": [
            "Feature has no mapping in feature-mappings.yaml; proceed file-centric; seed stub before Phase B"
        ],
    }


def test_allow_missing_unset_preserves_legacy_error() -> None:
    result = subprocess.run(
        ["python3", "scripts/kg/lookup.py", "F9999"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )

    assert result.returncode != 0
    assert "Unknown target: F9999" in result.stderr


def test_fields_ids_strip_rationale_and_source_docs(bundle: dict[str, object]) -> None:
    payload = _lookup_target(bundle, tier=3, fields="ids")

    workflow = _find_node(payload["affects"], "workflow:submission")
    assert workflow == {
        "id": "workflow:submission",
        "label": "Submission lifecycle",
    }


def test_hint_emission_fires_on_ambiguous_fixture(
    bundle: dict[str, object],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    fixture_scope = {
        "id": "feature:F9998",
        "path": "planning-mds/features/F9998-fixture",
        "status": "draft",
        "affects": [
            {
                "id": "entity:submission",
                "provenance": "ambiguous",
            }
        ],
    }

    monkeypatch.setattr(
        lookup,
        "feature_or_story_by_id",
        lambda normalized, mappings: dict(fixture_scope) if normalized == "feature:F9998" else None,
    )

    payload = lookup.lookup_by_target(
        "F9998",
        bundle,
        tier=1,
        fields="full",
        allow_missing=False,
    )

    assert payload["hints"] == [
        "1 ambiguous nodes detected (entity:submission) -- consider --tier 3 or open source_docs directly"
    ]
