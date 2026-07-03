import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.errors import CardValidationError
from app.orchestration.agent_card import AgentCard, load_cards

_VALID = {
    "card_id": "crm.renewals.head",
    "card_version": "1.0.0",
    "kind": "specialist_head",
    "name": "Renewals Head",
    "accepted_output_modes": ["app", "status"],
    "public": False,
}


class LoadShippedCardsTest(unittest.TestCase):
    def setUp(self):
        self.cards = load_cards(load_settings().cards_dir)

    def test_all_eight_cards_load(self):
        self.assertEqual(len(self.cards), 8)
        self.assertIn("neuron.orchestrator", self.cards)
        self.assertIn("crm.renewals.head", self.cards)
        self.assertIn("crm.renewals.outreach_drafter", self.cards)

    def test_no_public_card(self):
        # F0038 exposes no public Agent Card (ADR-027 §2).
        self.assertTrue(all(not c.public for c in self.cards.values()))

    def test_stub_heads_marked_inactive(self):
        for stub in ("crm.tasks.head", "crm.pipeline.head", "crm.broker_activity.head"):
            self.assertFalse(self.cards[stub].active, stub)
        self.assertTrue(self.cards["crm.renewals.head"].active)

    def test_user_token_only_on_engine_touching_agents(self):
        self.assertEqual(self.cards["crm.renewals.head"].auth_mode, "user_token")
        # Pure-classification / stub agents make no engine call.
        self.assertEqual(self.cards["crm.scope_guard"].auth_mode, "none")
        self.assertEqual(self.cards["crm.tasks.head"].auth_mode, "none")


class CardValidationTest(unittest.TestCase):
    def test_valid_card_hash_is_stable(self):
        a = AgentCard.from_dict(dict(_VALID))
        b = AgentCard.from_dict(dict(_VALID))
        self.assertEqual(a.content_hash, b.content_hash)
        self.assertTrue(a.content_hash.startswith("sha256:"))

    def test_missing_required_field_rejected(self):
        bad = dict(_VALID)
        del bad["kind"]
        with self.assertRaises(CardValidationError):
            AgentCard.from_dict(bad)

    def test_bad_card_id_rejected(self):
        bad = dict(_VALID, card_id="Not A Card Id")
        with self.assertRaises(CardValidationError):
            AgentCard.from_dict(bad)

    def test_bad_version_rejected(self):
        bad = dict(_VALID, card_version="v1")
        with self.assertRaises(CardValidationError):
            AgentCard.from_dict(bad)

    def test_public_true_rejected(self):
        bad = dict(_VALID, public=True)
        with self.assertRaises(CardValidationError):
            AgentCard.from_dict(bad)

    def test_unknown_kind_rejected(self):
        bad = dict(_VALID, kind="wizard")
        with self.assertRaises(CardValidationError):
            AgentCard.from_dict(bad)


if __name__ == "__main__":
    unittest.main()
