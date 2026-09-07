"""Regression tests for the two evidence-package profiles."""

from __future__ import annotations

import importlib.util
import tempfile
import unittest
from datetime import date
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "validate-evidence-package.py"
SPEC = importlib.util.spec_from_file_location("validate_evidence_package", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class EvidenceProfileTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.runs = self.root / "planning-mds/operations/evidence/runs"
        self.runs.mkdir(parents=True)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def test_base_run_without_manifest_is_allowed(self) -> None:
        run = self.runs / "2026-09-07-repository-validation"
        run.mkdir()
        (run / "gate-state.json").write_text("{}\n", encoding="utf-8")
        (run / "lifecycle-gates.log").write_text("Stage: validation\n", encoding="utf-8")

        errors = MODULE.check_run(self.root, run.name, date(2026, 9, 7))

        self.assertEqual(errors, [])

    def test_feature_completion_without_manifest_is_rejected(self) -> None:
        run = self.runs / "2026-09-07-feature-closeout"
        run.mkdir()
        (run / "pm-closeout.md").write_text("closeout\n", encoding="utf-8")

        errors = MODULE.check_run(self.root, run.name, date(2026, 9, 7))

        self.assertEqual(errors, [
            "2026-09-07-feature-closeout: missing evidence-manifest.json for a feature-completion run"
        ])


if __name__ == "__main__":
    unittest.main()
