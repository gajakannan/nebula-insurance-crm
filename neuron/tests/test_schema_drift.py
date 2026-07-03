import json
import os
import sys
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.schemas import CONTRACTS_DIR, get_validator

NEURON_ROOT = Path(__file__).resolve().parents[1]
PLANNING_SCHEMAS = NEURON_ROOT.parent / "planning-mds" / "schemas"

_VENDORED = [
    "neuron-agent-card.schema.json",
    "neuron-orchestration-plan.schema.json",
    "neuron-message-envelope.schema.json",
    "neuron-zone-payload.schema.json",
]


class SchemaDriftTest(unittest.TestCase):
    def test_vendored_contracts_match_planning_source(self):
        for name in _VENDORED:
            vendored = json.loads((CONTRACTS_DIR / name).read_text(encoding="utf-8"))
            source = json.loads((PLANNING_SCHEMAS / name).read_text(encoding="utf-8"))
            self.assertEqual(
                vendored, source,
                f"vendored {name} drifted from planning-mds/schemas — re-vendor it",
            )

    def test_all_vendored_schemas_compile(self):
        for key in ("agent-card", "orchestration-plan", "message-envelope", "zone-payload"):
            self.assertIsNotNone(get_validator(key))


if __name__ == "__main__":
    unittest.main()
