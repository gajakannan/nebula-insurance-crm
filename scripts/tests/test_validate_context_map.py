from __future__ import annotations

import importlib.util
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "validate-context-map.py"
SPEC = importlib.util.spec_from_file_location("validate_context_map", SCRIPT_PATH)
assert SPEC is not None
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def minimal_valid_map() -> dict:
    layers = {
        "product_core": {"default": True, "load": ["README.md"]},
        "feature_scope": {
            "default": True,
            "routing": "target-feature-only",
            "load": ["planning-mds/features/{FEATURE_ID}-*/README.md"],
        },
        "architecture_scope": {"default": False, "routing": "exact-file-or-topic"},
        "knowledge_graph_scope": {"default": True, "routing": "kg-first", "load": []},
        "api_schema_scope": {"default": False, "routing": "exact-contract-or-schema"},
        "frontend_scope": {"default": False, "routing": "changed-path-or-kg"},
        "backend_scope": {"default": False, "routing": "changed-path-or-kg"},
        "neuron_scope": {"default": False, "routing": "changed-path-or-kg"},
        "evidence_scope": {"default": False, "routing": "manifest-first-exact-file"},
        "archive_scope": {"default": False, "routing": "explicit-user-or-regression-audit"},
        "output_format": {"default": True, "load": ["planning-mds/features/REGISTRY.md"]},
        "examples": {"default": False, "routing": "explicit-user-or-template-need"},
    }
    return {
        "default_prompt_context": {
            "layers": ["product_core", "feature_scope", "knowledge_graph_scope", "output_format"]
        },
        "layers": layers,
        "never_default_prompt_context": ["planning-mds/operations/evidence/**"],
    }


def test_valid_context_map_passes() -> None:
    assert MODULE.validate_context_map(minimal_valid_map()) == []


def test_missing_required_layer_fails() -> None:
    data = minimal_valid_map()
    del data["layers"]["backend_scope"]

    errors = MODULE.validate_context_map(data)

    assert any("missing required layers" in error for error in errors)


def test_default_broad_source_glob_fails() -> None:
    data = minimal_valid_map()
    data["layers"]["product_core"]["load"].append("engine/**")

    errors = MODULE.validate_context_map(data)

    assert any("broad/high-token path" in error for error in errors)


def test_feature_scope_must_be_target_scoped() -> None:
    data = minimal_valid_map()
    data["layers"]["feature_scope"]["load"] = ["planning-mds/features/**"]

    errors = MODULE.validate_context_map(data)

    assert any("feature_scope must be target-scoped" in error for error in errors)


def test_on_demand_layer_requires_safe_routing() -> None:
    data = minimal_valid_map()
    data["layers"]["archive_scope"]["routing"] = "load broadly"

    errors = MODULE.validate_context_map(data)

    assert any("must require exact-file" in error for error in errors)
