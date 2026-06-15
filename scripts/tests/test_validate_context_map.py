from __future__ import annotations

import importlib.util
from pathlib import Path

import yaml


SCRIPT = Path(__file__).resolve().parents[1] / "validate-context-map.py"
spec = importlib.util.spec_from_file_location("validate_context_map", SCRIPT)
module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(module)


def minimal_context_map() -> dict:
    layers = {}
    for layer in module.REQUIRED_LAYERS:
        layers[layer] = {
            "default_context": [],
            "on_demand_context": ["some/exact-file.md"],
            "routing_required": ["exact_file"],
        }
    layers["product_core"]["default_context"] = ["README.md", "lifecycle-stage.yaml", ".agentignore"]
    layers["feature_scope"]["target_scoped"] = True
    layers["feature_scope"]["default_context"] = [
        "planning-mds/features/REGISTRY.md",
        "planning-mds/features/ROADMAP.md",
    ]
    return {
        "version": 1,
        "routing_modes": {mode: mode for mode in module.ROUTING_MODES},
        "layers": layers,
    }


def write_map(tmp_path: Path, data: dict) -> Path:
    path = tmp_path / "context-map.yaml"
    path.write_text(yaml.safe_dump(data), encoding="utf-8")
    return path


def test_valid_minimal_context_map_passes(tmp_path: Path) -> None:
    path = write_map(tmp_path, minimal_context_map())
    assert module.validate_context_map(path) == []


def test_default_archive_glob_fails(tmp_path: Path) -> None:
    data = minimal_context_map()
    data["layers"]["archive_scope"]["default_context"] = ["planning-mds/features/archive/**"]
    path = write_map(tmp_path, data)
    errors = module.validate_context_map(path)
    assert any("archive_scope" in error and "forbidden" in error for error in errors)


def test_default_source_tree_fails(tmp_path: Path) -> None:
    data = minimal_context_map()
    data["layers"]["backend_scope"]["default_context"] = ["engine/**"]
    path = write_map(tmp_path, data)
    errors = module.validate_context_map(path)
    assert any("backend_scope" in error and "source tree" in error for error in errors)


def test_on_demand_layer_requires_routing(tmp_path: Path) -> None:
    data = minimal_context_map()
    data["layers"]["evidence_scope"]["routing_required"] = []
    path = write_map(tmp_path, data)
    errors = module.validate_context_map(path)
    assert any("evidence_scope" in error and "routing_required" in error for error in errors)


def test_feature_scope_must_be_target_scoped(tmp_path: Path) -> None:
    data = minimal_context_map()
    data["layers"]["feature_scope"]["target_scoped"] = False
    path = write_map(tmp_path, data)
    errors = module.validate_context_map(path)
    assert any("feature_scope: target_scoped must be true" in error for error in errors)
