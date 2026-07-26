"""F0039-S0005 — catalog, prompt registry, and the composed resolution contract.

The catalog tests are authorization tests wearing a config-validation costume: every
cross-check exists so a mis-specified catalog cannot make something executable that the
reviewer did not intend. They assert the *refusal*, not just the parse.
"""

from __future__ import annotations

import os
import sys
import textwrap
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.intent.catalog import CatalogError, load_catalog
from app.intent.prompt_registry import (
    INTENT_CLASSIFIER_PROMPT,
    INTENT_RESOLVER_PROMPT,
    SCOPE_GUARD_PROMPT,
    PromptError,
    PromptRegistry,
)

NEURON_ROOT = Path(__file__).resolve().parents[1]
SHIPPED_CATALOG = NEURON_ROOT / "config" / "intent-catalog.yaml"
HEADS = {
    "crm.renewals.head",
    "crm.tasks.head",
    "crm.pipeline.head",
    "crm.broker_activity.head",
}

VALID = """
catalog_version: 1.0.0
domains:
  renewals:
    target_head_card_id: crm.renewals.head
    active: true
    description: Renewals.
    actions:
      renewals.list_attention:
        active: true
        description: List renewals needing attention.
        required_entities: []
      renewals.view:
        active: true
        description: View one renewal.
        required_entities:
          - one_of: [renewal_id, policy_number]
"""


def write(tmp: Path, body: str) -> Path:
    path = tmp / "intent-catalog.yaml"
    path.write_text(textwrap.dedent(body), encoding="utf-8")
    return path


class ShippedCatalogTest(unittest.TestCase):
    def setUp(self):
        self.catalog = load_catalog(SHIPPED_CATALOG, registered_head_ids=HEADS)

    def test_loads_and_records_a_content_hash(self):
        self.assertEqual(self.catalog.catalog_version, "1.0.0")
        self.assertTrue(self.catalog.content_hash.startswith("sha256:"))

    def test_only_renewals_is_active_today(self):
        self.assertEqual(self.catalog.active_domain_ids(), ["renewals"])

    def test_inactive_domain_actions_are_not_executable(self):
        # tasks/pipeline/broker_activity exist as definitions but must not be routable.
        self.assertNotIn("tasks.list", self.catalog.active_action_ids())

    def test_head_is_resolved_from_the_catalog(self):
        self.assertEqual(self.catalog.head_for("renewals"), "crm.renewals.head")

    def test_write_like_action_requires_confirmation(self):
        action = self.catalog.action("renewals.mock_send")
        self.assertTrue(action.requires_explicit_confirmation)

    def test_prompt_rendering_is_deterministic(self):
        first = self.catalog.describe_for_prompt()
        second = load_catalog(SHIPPED_CATALOG, registered_head_ids=HEADS).describe_for_prompt()
        self.assertEqual(first, second)

    def test_prompt_rendering_omits_inactive_domains(self):
        rendered = self.catalog.describe_for_prompt()
        self.assertIn("renewals.list_attention", rendered)
        self.assertNotIn("tasks.list", rendered)


class CatalogCrossCheckTest(unittest.TestCase):
    """Each of these would let something unintended become routable."""

    def setUp(self):
        import tempfile

        self._tmp = tempfile.TemporaryDirectory()
        self.tmp = Path(self._tmp.name)

    def tearDown(self):
        self._tmp.cleanup()

    def test_valid_catalog_loads(self):
        catalog = load_catalog(write(self.tmp, VALID), registered_head_ids=HEADS)
        self.assertEqual(catalog.active_domain_ids(), ["renewals"])

    def test_unregistered_head_is_refused(self):
        body = VALID.replace("crm.renewals.head", "crm.ghost.head")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_missing_head_id_is_refused(self):
        body = VALID.replace("    target_head_card_id: crm.renewals.head\n", "")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_action_must_be_prefixed_with_its_domain(self):
        body = VALID.replace("renewals.view:", "tasks.view:")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_duplicate_action_id_is_refused(self):
        # A second domain re-declaring an existing action id. Indented to sit *under*
        # `domains:` — at column 0 it would be a sibling key and silently ignored.
        body = VALID + (
            "  tasks:\n"
            "    target_head_card_id: crm.tasks.head\n"
            "    active: false\n"
            "    actions:\n"
            "      renewals.view:\n"
            "        active: false\n"
            "        description: duplicate id under another domain.\n"
        )
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_unregistered_entity_type_is_refused(self):
        body = VALID.replace("[renewal_id, policy_number]", "[social_security_number]")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_active_action_under_inactive_domain_is_refused(self):
        body = VALID.replace("    active: true\n    description: Renewals.", "    active: false\n    description: Renewals.")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_active_domain_with_no_active_action_is_refused(self):
        body = VALID.replace("        active: true\n        description: List renewals needing attention.", "        active: false\n        description: List renewals needing attention.")
        body = body.replace("        active: true\n        description: View one renewal.", "        active: false\n        description: View one renewal.")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_catalog_with_no_active_domain_is_refused(self):
        body = VALID.replace("    active: true\n", "    active: false\n")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_missing_catalog_version_is_refused(self):
        body = VALID.replace("catalog_version: 1.0.0\n", "")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)

    def test_malformed_yaml_is_refused(self):
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, "catalog_version: [unclosed\n"), registered_head_ids=HEADS)

    def test_absent_file_is_refused(self):
        with self.assertRaises(CatalogError):
            load_catalog(self.tmp / "nope.yaml", registered_head_ids=HEADS)

    def test_malformed_required_entities_is_refused(self):
        body = VALID.replace("          - one_of: [renewal_id, policy_number]", "          - renewal_id")
        with self.assertRaises(CatalogError):
            load_catalog(write(self.tmp, body), registered_head_ids=HEADS)


class EntityRequirementTest(unittest.TestCase):
    def setUp(self):
        self.catalog = load_catalog(SHIPPED_CATALOG, registered_head_ids=HEADS)

    def test_one_of_is_satisfied_by_any_member(self):
        action = self.catalog.action("renewals.view")
        self.assertEqual(action.missing_entities({"policy_number": "P-1"}), [])

    def test_missing_every_member_is_unsatisfied(self):
        action = self.catalog.action("renewals.view")
        self.assertEqual(len(action.missing_entities({})), 1)

    def test_null_entity_does_not_satisfy(self):
        action = self.catalog.action("renewals.view")
        self.assertEqual(len(action.missing_entities({"renewal_id": None})), 1)

    def test_action_with_no_requirements_is_always_satisfied(self):
        action = self.catalog.action("renewals.list_attention")
        self.assertEqual(action.missing_entities({}), [])


class PromptRegistryTest(unittest.TestCase):
    def setUp(self):
        self.registry = PromptRegistry(NEURON_ROOT / "prompts")

    def test_loads_the_composed_resolver_prompt_with_a_hash(self):
        prompt = self.registry.load(INTENT_RESOLVER_PROMPT)
        self.assertTrue(prompt.text.strip())
        self.assertTrue(prompt.content_hash.startswith("sha256:"))
        self.assertEqual(prompt.reference, "crm-intent-resolver@1.0.0")

    def test_loads_both_fragments(self):
        loaded = self.registry.load_all([SCOPE_GUARD_PROMPT, INTENT_CLASSIFIER_PROMPT])
        self.assertEqual(len(loaded), 2)

    def test_metadata_is_available_for_provenance(self):
        prompt = self.registry.load(INTENT_RESOLVER_PROMPT)
        self.assertEqual(prompt.metadata["output_schema"], "neuron-intent-resolution.schema.json")

    def test_hash_is_stable_across_loads(self):
        first = PromptRegistry(NEURON_ROOT / "prompts").load(INTENT_RESOLVER_PROMPT)
        second = PromptRegistry(NEURON_ROOT / "prompts").load(INTENT_RESOLVER_PROMPT)
        self.assertEqual(first.content_hash, second.content_hash)

    def test_resolver_prompt_carries_the_catalog_placeholder(self):
        prompt = self.registry.load(INTENT_RESOLVER_PROMPT)
        self.assertIn("{catalog}", prompt.text)

    def test_missing_prompt_is_refused(self):
        with self.assertRaises(PromptError):
            self.registry.load("crm-does-not-exist")

    def test_missing_version_is_refused(self):
        with self.assertRaises(PromptError):
            self.registry.load(INTENT_RESOLVER_PROMPT, version="9.9.9")

    def test_empty_prompt_is_refused(self):
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp) / "crm-empty" / "1.0.0"
            directory.mkdir(parents=True)
            (directory / "system.md").write_text("   \n", encoding="utf-8")
            with self.assertRaises(PromptError):
                PromptRegistry(Path(tmp)).load("crm-empty")

    def test_metadata_version_must_match_its_directory(self):
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp) / "crm-skewed" / "1.0.0"
            directory.mkdir(parents=True)
            (directory / "system.md").write_text("hello", encoding="utf-8")
            (directory / "metadata.yaml").write_text("version: 2.0.0\n", encoding="utf-8")
            with self.assertRaises(PromptError):
                PromptRegistry(Path(tmp)).load("crm-skewed")


class StartupWiringTest(unittest.TestCase):
    """An invalid catalog or missing prompt must prevent readiness (story edge case)."""

    def test_runtime_loads_catalog_and_prompts(self):
        from app.bootstrap import build_runtime

        runtime = build_runtime()
        self.assertIsNotNone(runtime.intent_catalog)
        self.assertEqual(runtime.intent_catalog.catalog_version, "1.0.0")
        self.assertIn(INTENT_RESOLVER_PROMPT, runtime.prompts)

    def test_readiness_reports_the_loaded_catalog_and_prompts(self):
        from app.bootstrap import build_runtime

        _, detail = build_runtime().readiness()
        self.assertEqual(detail["intent_catalog_version"], "1.0.0")
        self.assertIn("crm-intent-resolver@1.0.0", detail["prompts"])

    def test_invalid_catalog_stops_startup(self):
        import tempfile

        from app.bootstrap import build_runtime

        with tempfile.TemporaryDirectory() as tmp:
            bad = Path(tmp) / "intent-catalog.yaml"
            bad.write_text("catalog_version: 1.0.0\ndomains: {}\n", encoding="utf-8")
            settings = load_settings()
            broken = type(settings)(
                **{**vars(settings), "intent_catalog_path": bad}
            )
            with self.assertRaises(CatalogError):
                build_runtime(broken)

    def test_missing_prompts_dir_stops_startup(self):
        import tempfile

        from app.bootstrap import build_runtime

        with tempfile.TemporaryDirectory() as tmp:
            settings = load_settings()
            broken = type(settings)(**{**vars(settings), "prompts_dir": Path(tmp)})
            with self.assertRaises(PromptError):
                build_runtime(broken)


if __name__ == "__main__":
    unittest.main()
