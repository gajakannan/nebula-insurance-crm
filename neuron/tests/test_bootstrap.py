import os
import sys
import tempfile
import unittest
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.bootstrap import build_runtime
from app.config import load_settings
from app.errors import CardValidationError, ConfigError


class BootstrapHappyPathTest(unittest.TestCase):
    def setUp(self):
        self.runtime = build_runtime()

    def test_health_snapshot_lists_heads_and_tools(self):
        snap = self.runtime.health_snapshot()
        self.assertEqual(snap["status"], "healthy")
        self.assertEqual(
            snap["heads"],
            ["crm.broker_activity.head", "crm.pipeline.head", "crm.renewals.head", "crm.tasks.head"],
        )
        self.assertEqual(len(snap["tools"]), 5)

    def test_ready(self):
        ok, detail = self.runtime.readiness()
        self.assertTrue(ok)
        self.assertIn("day-at-a-glance", detail["plans"])
        self.assertEqual(detail["model_provider"], "mock")

    def test_plan_loaded(self):
        self.assertIn("day-at-a-glance", self.runtime.plans)


class BootstrapFailFastTest(unittest.TestCase):
    def test_missing_plans_dir_fails_fast(self):
        with tempfile.TemporaryDirectory() as tmp:
            settings = replace(load_settings(), plans_dir=Path(tmp))
            with self.assertRaises(ConfigError):
                build_runtime(settings)

    def test_invalid_card_fails_fast(self):
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / "broken.yaml").write_text("card_id: not valid id\nkind: wizard\n", encoding="utf-8")
            settings = replace(load_settings(), cards_dir=Path(tmp))
            with self.assertRaises(CardValidationError):
                build_runtime(settings)

    def test_unsupported_persistence_backend_fails_fast(self):
        settings = replace(load_settings(), persistence_backend="postgres")
        with self.assertRaises(ConfigError):
            build_runtime(settings)

    def test_unwired_model_provider_fails_fast(self):
        settings = replace(load_settings(), model_provider="claude")
        with self.assertRaises(ConfigError):
            build_runtime(settings)


if __name__ == "__main__":
    unittest.main()
