import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import jsonschema

from app import envelope as env
from app.envelope import UnknownActionError, UnknownComponentError
from app.schemas import get_validator


class PartBuilderTest(unittest.TestCase):
    def test_text_and_status_parts(self):
        self.assertEqual(env.text_part("hi"), {"part_type": "text", "text": "hi"})
        self.assertEqual(env.status_part("completed"), {"part_type": "status", "state": "completed"})
        self.assertEqual(env.status_part("working", "loading")["detail"], "loading")

    def test_app_part_requires_registered_component(self):
        part = env.app_part("renewals.needs_attention_list", {"items": []})
        self.assertEqual(part["component"], "renewals.needs_attention_list")
        with self.assertRaises(UnknownComponentError):
            env.app_part("evil.script", {"x": 1})

    def test_actions_part_requires_registered_action(self):
        ok = env.actions_part([{"action_id": "a1", "label": "Draft", "action_type": "draft_outreach"}])
        self.assertEqual(ok["part_type"], "actions")
        with self.assertRaises(UnknownActionError):
            env.actions_part([{"action_id": "a1", "label": "x", "action_type": "rm_rf"}])


class EnvelopeBuilderTest(unittest.TestCase):
    def test_build_envelope_is_schema_valid(self):
        e = env.build_envelope(
            "11111111-1111-1111-1111-111111111111",
            role="assistant",
            parts=[env.status_part("completed"), env.text_part("Here's your day.")],
        )
        get_validator("message-envelope").validate(e)  # raises if invalid
        self.assertEqual(e["envelope_version"], 1)
        self.assertEqual(e["role"], "assistant")
        self.assertEqual(len(e["parts"]), 2)

    def test_build_envelope_rejects_malformed_part(self):
        with self.assertRaises(jsonschema.ValidationError):
            env.build_envelope(
                "11111111-1111-1111-1111-111111111111",
                role="assistant",
                parts=[{"part_type": "text"}],  # missing 'text'
            )

    def test_build_envelope_rejects_unknown_top_level_field(self):
        # message-envelope schema is additionalProperties:false — an app part with a
        # non-registered component would already be blocked by app_part(); this guards
        # the envelope shape itself.
        with self.assertRaises(jsonschema.ValidationError):
            get_validator("message-envelope").validate(
                {
                    "envelope_version": 1,
                    "thread_id": "11111111-1111-1111-1111-111111111111",
                    "message_id": "22222222-2222-2222-2222-222222222222",
                    "role": "assistant",
                    "parts": [env.text_part("x")],
                    "unexpected": True,
                }
            )


if __name__ == "__main__":
    unittest.main()
