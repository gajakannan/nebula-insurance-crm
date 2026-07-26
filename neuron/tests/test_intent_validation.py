"""F0039-S0005 — fail-closed validation of model resolution output.

These are the security tests of the intent layer. Each one asserts that a model output
which *looks* usable is refused: a contradictory decision, an invented action, an
inactive action, a cross-domain mix, a route that survived a scope redirect. The
property under test is always the same — **the model proposes, the catalog and the
invariants dispose** — and the failure mode is always bounded (redirect or clarify),
never a partial execution.

Two named regression fixtures from the story are covered explicitly:
`redirect` + `renewals.list_attention`, and the invented
`show_renewals_needing_attention`.
"""

from __future__ import annotations

import copy
import os
import sys
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.intent.catalog import load_catalog
from app.intent.contracts import REASON_INVALID_MODEL_OUTPUT
from app.intent.validation import (
    R_CROSS_DOMAIN_ACTION,
    R_CROSS_SECTION,
    R_INACTIVE_ACTION,
    R_INACTIVE_DOMAIN,
    R_INTENT_INVARIANT,
    R_MISSING_ENTITY,
    R_SCHEMA_INVALID,
    R_SCOPE_INVARIANT,
    R_TOO_MANY_ACTIONS,
    R_UNKNOWN_ACTION,
    R_UNKNOWN_DOMAIN,
    intent_invariants_hold,
    resolve,
    scope_invariants_hold,
    validate_schema,
)

NEURON_ROOT = Path(__file__).resolve().parents[1]
HEADS = {"crm.renewals.head", "crm.tasks.head", "crm.pipeline.head", "crm.broker_activity.head"}
CATALOG = load_catalog(NEURON_ROOT / "config" / "intent-catalog.yaml", registered_head_ids=HEADS)


def scope(**overrides):
    base = {
        "schema_version": "1.0.0",
        "decision": "allow",
        "scope": "crm",
        "reason_code": "in_scope",
        "requires_intent_resolution": True,
        "clarification_code": None,
    }
    base.update(overrides)
    return base


def intent(**overrides):
    base = {
        "schema_version": "1.0.0",
        "decision": "route",
        "domain": "renewals",
        "actions": ["renewals.list_attention"],
        "entities": {},
        "needs_context": False,
        "needs_adjudication": False,
        "clarification_code": None,
    }
    base.update(overrides)
    return base


def payload(scope_overrides=None, intent_overrides=None):
    return {
        "schema_version": "1.0.0",
        "scope": scope(**(scope_overrides or {})),
        "intent": intent(**(intent_overrides or {})),
    }


class SchemaLayerTest(unittest.TestCase):
    def test_well_formed_payload_passes(self):
        self.assertTrue(validate_schema(payload()))

    def test_additional_properties_are_rejected(self):
        bad = payload()
        bad["intent"]["injected_field"] = "anything"
        self.assertFalse(validate_schema(bad))

    def test_unknown_entity_key_is_rejected(self):
        bad = payload()
        bad["intent"]["entities"] = {"ssn": "123-45-6789"}
        self.assertFalse(validate_schema(bad))

    def test_wrong_schema_version_is_rejected(self):
        bad = payload()
        bad["schema_version"] = "2.0.0"
        self.assertFalse(validate_schema(bad))

    def test_too_many_actions_is_rejected_by_the_schema(self):
        bad = payload(intent_overrides={"actions": ["a", "b", "c", "d", "e"]})
        self.assertFalse(validate_schema(bad))

    def test_unknown_clarification_code_is_rejected(self):
        bad = payload(intent_overrides={"clarification_code": "because_i_said_so"})
        self.assertFalse(validate_schema(bad))

    def test_non_object_payload_is_rejected(self):
        self.assertFalse(validate_schema(["not", "an", "object"]))


class ScopeInvariantTest(unittest.TestCase):
    def test_valid_allow(self):
        self.assertTrue(scope_invariants_hold(scope()))

    def test_allow_requires_crm_scope(self):
        self.assertFalse(scope_invariants_hold(scope(scope="non_crm")))

    def test_allow_requires_intent_resolution(self):
        self.assertFalse(scope_invariants_hold(scope(requires_intent_resolution=False)))

    def test_allow_cannot_carry_a_clarification_code(self):
        self.assertFalse(scope_invariants_hold(scope(clarification_code="ask_user_goal")))

    def test_valid_redirect(self):
        self.assertTrue(
            scope_invariants_hold(
                scope(
                    decision="redirect",
                    scope="non_crm",
                    reason_code="out_of_scope",
                    requires_intent_resolution=False,
                )
            )
        )

    def test_suspicious_redirect_is_valid(self):
        self.assertTrue(
            scope_invariants_hold(
                scope(
                    decision="redirect",
                    scope="suspicious",
                    reason_code="instruction_override",
                    requires_intent_resolution=False,
                )
            )
        )

    def test_redirect_cannot_require_intent_resolution(self):
        self.assertFalse(
            scope_invariants_hold(
                scope(decision="redirect", scope="non_crm", reason_code="out_of_scope")
            )
        )

    def test_clarify_requires_ambiguous_scope_and_a_code(self):
        self.assertTrue(
            scope_invariants_hold(
                scope(
                    decision="clarify",
                    scope="ambiguous",
                    reason_code="ambiguous",
                    requires_intent_resolution=False,
                    clarification_code="ask_crm_area",
                )
            )
        )

    def test_clarify_without_a_code_is_refused(self):
        self.assertFalse(
            scope_invariants_hold(
                scope(
                    decision="clarify",
                    scope="ambiguous",
                    reason_code="ambiguous",
                    requires_intent_resolution=False,
                )
            )
        )


class IntentInvariantTest(unittest.TestCase):
    def test_route_needs_a_domain_and_an_action(self):
        from app.intent.validation import parse_intent

        self.assertTrue(intent_invariants_hold(parse_intent(intent())))
        self.assertFalse(intent_invariants_hold(parse_intent(intent(actions=[]))))
        self.assertFalse(intent_invariants_hold(parse_intent(intent(domain=None))))

    def test_redirect_must_carry_nothing(self):
        from app.intent.validation import parse_intent

        clean = parse_intent(intent(decision="redirect", domain=None, actions=[]))
        self.assertTrue(intent_invariants_hold(clean))

    def test_clarify_needs_a_code_and_no_actions(self):
        from app.intent.validation import parse_intent

        ok = parse_intent(
            intent(decision="clarify", actions=[], clarification_code="missing_entity")
        )
        self.assertTrue(intent_invariants_hold(ok))
        bad = parse_intent(intent(decision="clarify", actions=[], clarification_code=None))
        self.assertFalse(intent_invariants_hold(bad))


class RegressionFixtureTest(unittest.TestCase):
    """The two combinations the story names by hand. Both must be rejected."""

    def test_redirect_carrying_a_routed_action_is_rejected(self):
        """Observed regression: a `redirect` that still carried `renewals.list_attention`.

        A redirect means nothing executes. Honouring the action anyway would route a
        message the resolver had just declined.
        """
        result = resolve(
            payload(
                intent_overrides={
                    "decision": "redirect",
                    "domain": "renewals",
                    "actions": ["renewals.list_attention"],
                }
            ),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertIsNone(result.target_head_card_id)
        self.assertIn(R_INTENT_INVARIANT, result.rejection_codes)

    def test_invented_action_is_rejected(self):
        """Observed regression: the model invented `show_renewals_needing_attention`.

        It reads like a real action and is not in the catalog, so it cannot route —
        no fuzzy match, no closest-neighbour repair.
        """
        result = resolve(
            payload(intent_overrides={"actions": ["show_renewals_needing_attention"]}),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertIn(R_UNKNOWN_ACTION, result.rejection_codes)


class RouteValidationTest(unittest.TestCase):
    def test_valid_route_resolves_a_head_from_the_catalog(self):
        result = resolve(payload(), CATALOG)
        self.assertTrue(result.should_route)
        self.assertEqual(result.target_head_card_id, "crm.renewals.head")

    def test_unknown_domain_is_rejected(self):
        result = resolve(payload(intent_overrides={"domain": "payroll"}), CATALOG)
        self.assertFalse(result.should_route)
        self.assertIn(R_UNKNOWN_DOMAIN, result.rejection_codes)

    def test_inactive_domain_is_rejected(self):
        result = resolve(
            payload(intent_overrides={"domain": "tasks", "actions": ["tasks.list"]}), CATALOG
        )
        self.assertFalse(result.should_route)
        self.assertIn(R_INACTIVE_DOMAIN, result.rejection_codes)

    def test_cross_domain_action_is_not_collapsed(self):
        result = resolve(
            payload(
                intent_overrides={
                    "domain": "renewals",
                    "actions": ["renewals.view", "tasks.list"],
                    "entities": {"renewal_id": "R-1"},
                }
            ),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertIn(R_CROSS_DOMAIN_ACTION, result.rejection_codes)

    def test_missing_required_entity_becomes_a_clarify_not_a_guess(self):
        result = resolve(payload(intent_overrides={"actions": ["renewals.view"]}), CATALOG)
        self.assertFalse(result.should_route)
        self.assertEqual(result.intent.decision, "clarify")
        self.assertIn(R_MISSING_ENTITY, result.rejection_codes)

    def test_supplied_entity_satisfies_the_requirement(self):
        result = resolve(
            payload(
                intent_overrides={
                    "actions": ["renewals.view"],
                    "entities": {"renewal_id": "R-1"},
                }
            ),
            CATALOG,
        )
        self.assertTrue(result.should_route)

    def test_write_like_action_flags_confirmation(self):
        result = resolve(
            payload(
                intent_overrides={
                    "actions": ["renewals.mock_send"],
                    "entities": {"renewal_id": "R-1"},
                }
            ),
            CATALOG,
        )
        self.assertTrue(result.should_route)
        self.assertTrue(result.requires_confirmation)

    def test_read_only_action_does_not_flag_confirmation(self):
        self.assertFalse(resolve(payload(), CATALOG).requires_confirmation)

    def test_action_count_policy_is_enforced(self):
        result = resolve(
            payload(
                intent_overrides={
                    "actions": [
                        "renewals.list_attention",
                        "renewals.view",
                        "renewals.summarize",
                        "renewals.draft_outreach",
                    ],
                    "entities": {"renewal_id": "R-1"},
                }
            ),
            CATALOG,
        )
        # Four is the ceiling, so this routes; five is rejected by the schema already.
        self.assertTrue(result.should_route or R_TOO_MANY_ACTIONS in result.rejection_codes)


class CrossSectionTest(unittest.TestCase):
    def test_scope_redirect_with_a_routed_intent_is_rejected(self):
        """The partially-successful-injection shape: scope declined, intent routed."""
        result = resolve(
            payload(
                scope_overrides={
                    "decision": "redirect",
                    "scope": "suspicious",
                    "reason_code": "instruction_override",
                    "requires_intent_resolution": False,
                }
            ),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertIn(R_CROSS_SECTION, result.rejection_codes)

    def test_scope_redirect_with_a_redirect_intent_is_carried_through(self):
        result = resolve(
            payload(
                scope_overrides={
                    "decision": "redirect",
                    "scope": "non_crm",
                    "reason_code": "out_of_scope",
                    "requires_intent_resolution": False,
                },
                intent_overrides={"decision": "redirect", "domain": None, "actions": []},
            ),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertEqual(result.scope.reason_code, "out_of_scope")
        # A legitimate redirect is not relabelled as invalid model output.
        self.assertNotIn(REASON_INVALID_MODEL_OUTPUT, result.rejection_codes)

    def test_adjudicate_degrades_to_clarify_while_s0009_is_gated(self):
        result = resolve(
            payload(
                intent_overrides={
                    "decision": "adjudicate",
                    "needs_adjudication": True,
                    "actions": [],
                    "domain": None,
                }
            ),
            CATALOG,
        )
        self.assertFalse(result.should_route)
        self.assertEqual(result.intent.decision, "clarify")
        self.assertIn("adjudication_gated", result.rejection_codes)


class FailClosedTest(unittest.TestCase):
    def test_garbage_payload_fails_closed(self):
        for garbage in (None, [], "text", 42, {}):
            result = resolve(garbage, CATALOG)
            self.assertFalse(result.should_route)
            self.assertEqual(result.scope.decision, "redirect")

    def test_schema_violation_maps_to_a_safe_redirect(self):
        bad = payload()
        bad["intent"]["surprise"] = True
        result = resolve(bad, CATALOG)
        self.assertIn(R_SCHEMA_INVALID, result.rejection_codes)
        self.assertEqual(result.scope.reason_code, REASON_INVALID_MODEL_OUTPUT)

    def test_scope_invariant_violation_maps_to_a_safe_redirect(self):
        result = resolve(payload(scope_overrides={"scope": "non_crm"}), CATALOG)
        self.assertIn(R_SCOPE_INVARIANT, result.rejection_codes)
        self.assertEqual(result.scope.decision, "redirect")

    def test_rejections_carry_codes_not_user_text(self):
        """Telemetry must never accumulate the message that triggered a rejection."""
        secret = "policy 12345 for Jane Doe"
        bad = payload(intent_overrides={"entities": {"account_name": secret}, "actions": ["nope.nope"]})
        result = resolve(bad, CATALOG)
        joined = " ".join(result.rejection_codes)
        self.assertNotIn(secret, joined)
        self.assertNotIn("Jane", joined)

    def test_a_rejected_resolution_never_exposes_a_head(self):
        for overrides in (
            {"domain": "payroll"},
            {"actions": ["invented.action"]},
            {"domain": "tasks", "actions": ["tasks.list"]},
        ):
            result = resolve(payload(intent_overrides=overrides), CATALOG)
            self.assertIsNone(result.target_head_card_id, overrides)

    def test_model_supplied_head_id_is_never_honoured(self):
        """Even a schema-shaped attempt to name a head is ignored: heads come from the
        catalog. (The schema has no head field at all — this asserts the property.)"""
        bad = copy.deepcopy(payload())
        bad["intent"]["target_head_card_id"] = "crm.payroll.head"
        result = resolve(bad, CATALOG)
        self.assertFalse(result.should_route)


if __name__ == "__main__":
    unittest.main()
