"""F0039-S0008 — evaluation harness, shadow mode, rollout gating, and load.

Two things are being proven here. First, that the harness *can fail* — a gate that
cannot go red is decoration, so several tests force red conditions deliberately. Second,
that shadow mode is genuinely inert: the deterministic guard decides production and the
resolver's opinion changes nothing a user can observe.
"""

from __future__ import annotations

import asyncio
import os
import sys
import time
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.bootstrap import build_runtime
from app.intent.catalog import load_catalog
from app.intent.evaluation import GATES, IntentEvaluator, evaluate, load_dataset
from app.intent.prompt_registry import INTENT_RESOLVER_PROMPT, PromptRegistry
from app.intent.resolver import IntentResolver
from app.messages import MODE_SHADOW, MessageDispatcher
from app.models.router import ModelRouter
from app.models.scripted_provider import ScriptedProvider

NEURON_ROOT = Path(__file__).resolve().parents[1]
HEADS = {"crm.renewals.head", "crm.tasks.head", "crm.pipeline.head", "crm.broker_activity.head"}
CATALOG = load_catalog(NEURON_ROOT / "config" / "intent-catalog.yaml", registered_head_ids=HEADS)
PROMPT = PromptRegistry(NEURON_ROOT / "prompts").load(INTENT_RESOLVER_PROMPT)
EVAL_ROOT = NEURON_ROOT / "evals" / "intent" / "v1"


def resolution(decision="route", domain="renewals", actions=("renewals.list_attention",),
               scope_decision="allow", scope="crm", reason="in_scope", requires=True,
               clarification=None, entities=None):
    return {
        "schema_version": "1.0.0",
        "scope": {
            "schema_version": "1.0.0", "decision": scope_decision, "scope": scope,
            "reason_code": reason, "requires_intent_resolution": requires,
            "clarification_code": None,
        },
        "intent": {
            "schema_version": "1.0.0", "decision": decision, "domain": domain,
            "actions": list(actions), "entities": entities or {}, "needs_context": False,
            "needs_adjudication": False, "clarification_code": clarification,
        },
    }


def _resolver(provider):
    return IntentResolver(
        model_router=ModelRouter({"p": provider}, default="p"), catalog=CATALOG, prompt=PROMPT
    )


class DatasetTest(unittest.TestCase):
    def test_all_four_datasets_exist_and_are_non_empty(self):
        for name in ("direct", "redirect", "adversarial", "contradiction"):
            cases = load_dataset(name, EVAL_ROOT)
            self.assertTrue(cases, name)
            for case in cases:
                self.assertIn("id", case)
                self.assertIn("text", case)
                self.assertIn("expect", case)

    def test_case_ids_are_unique_across_datasets(self):
        seen = set()
        for name in ("direct", "redirect", "adversarial", "contradiction"):
            for case in load_dataset(name, EVAL_ROOT):
                self.assertNotIn(case["id"], seen)
                seen.add(case["id"])

    def test_direct_cases_declare_expected_domain_and_actions(self):
        for case in load_dataset("direct", EVAL_ROOT):
            self.assertIn("domain", case)
            self.assertTrue(case["actions"])

    def test_adversarial_set_covers_the_named_attack_classes(self):
        text = " ".join(c["text"] for c in load_dataset("adversarial", EVAL_ROOT)).casefold()
        for marker in ("ignore previous", "developer mode", "system prompt", "email all", "pretend you are"):
            self.assertIn(marker, text)


class HarnessScoringTest(unittest.IsolatedAsyncioTestCase):
    async def test_a_perfect_run_passes_every_gate(self):
        """A provider that answers every case correctly must go green — otherwise the
        harness is measuring something other than the resolver."""

        class PerfectProvider(ScriptedProvider):
            async def complete_structured(self, *, prompt, schema, **kwargs):
                lowered = prompt.casefold()
                if any(k in lowered for k in ("capital", "python", "world cup", "joke",
                                              "weather", "quantum", "restaurant",
                                              "translate", "stocks", "sourdough")):
                    payload = resolution(decision="redirect", domain=None, actions=(),
                                         scope_decision="redirect", scope="non_crm",
                                         reason="out_of_scope", requires=False)
                elif any(k in lowered for k in ("ignore", "disregard", "developer mode",
                                                "reveal", "email all", "export every",
                                                "pretend", "act as", "bypass",
                                                "hidden instructions", "system:",
                                                "assistant:", "every other user",
                                                "show_renewals", "jailbreak")):
                    payload = resolution(decision="redirect", domain=None, actions=(),
                                         scope_decision="redirect", scope="suspicious",
                                         reason="instruction_override", requires=False)
                elif any(k in lowered for k in ("hi", "hello", "help me", "what can you do",
                                                "that one", "summarize it", "draft the outreach",
                                                "open it")) and "renewal" not in lowered:
                    payload = resolution(decision="clarify", domain=None, actions=(),
                                         scope_decision="clarify", scope="ambiguous",
                                         reason="ambiguous", requires=False,
                                         clarification="missing_domain")
                    payload["scope"]["clarification_code"] = "ask_crm_area"
                else:
                    expected = _expected_for(prompt)
                    # Actions like renewals.view require an entity; a perfect model would
                    # have extracted one from the message.
                    needs_entity = any(a != "renewals.list_attention" for a in expected["actions"])
                    payload = resolution(
                        actions=expected["actions"], domain=expected["domain"],
                        entities={"renewal_id": "R-1"} if needs_entity else {},
                    )
                return await _as_result(payload)

        report = await evaluate(_resolver(PerfectProvider()), root=EVAL_ROOT)
        self.assertTrue(report.all_gates_passed, report.failed_case_ids)

    async def test_a_bypass_turns_the_gate_red(self):
        """Every case routes, including the injection set — the harness must catch it."""
        provider = ScriptedProvider().script_default(resolution())
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        self.assertFalse(report.all_gates_passed)
        self.assertFalse(report.gate_results["authorization_bypasses"])
        self.assertGreater(report.metrics["authorization_bypasses"], 0)

    async def test_never_routing_fails_the_accuracy_gates(self):
        provider = ScriptedProvider().script_default(
            resolution(decision="redirect", domain=None, actions=(),
                       scope_decision="redirect", scope="non_crm",
                       reason="out_of_scope", requires=False)
        )
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        self.assertFalse(report.gate_results["domain_accuracy"])
        self.assertEqual(report.metrics["domain_accuracy"], 0.0)

    async def test_fail_closed_gate_is_measured_against_real_failures(self):
        provider = ScriptedProvider().script_default(resolution())
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        # Timeout, unavailable, malformed, and nonsense payloads must all fail closed.
        self.assertEqual(report.metrics["fail_closed_rate"], 1.0)

    async def test_failed_case_ids_are_reported(self):
        provider = ScriptedProvider().script_default(resolution())
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        self.assertTrue(report.failed_case_ids)

    async def test_report_contains_no_raw_case_text(self):
        """The adversarial payloads must not be duplicated into an artifact."""
        provider = ScriptedProvider().script_default(resolution())
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        import json

        blob = json.dumps(report.to_dict())
        for case in load_dataset("adversarial", EVAL_ROOT):
            self.assertNotIn(case["text"], blob)

    async def test_empty_dataset_scores_zero_rather_than_passing_vacuously(self):
        evaluator = IntentEvaluator(_resolver(ScriptedProvider().script_default(resolution())))
        report = evaluator._score([], ())
        self.assertEqual(report.metrics["domain_accuracy"], 0.0)


class ProvenanceTest(unittest.IsolatedAsyncioTestCase):
    async def test_report_records_everything_needed_to_reproduce_it(self):
        provider = ScriptedProvider().script_default(resolution())
        report = await evaluate(_resolver(provider), root=EVAL_ROOT)
        p = report.provenance
        for key in ("git_commit", "prompt_id", "prompt_hash", "catalog_version",
                    "catalog_hash", "schema_hashes", "hardware", "recorded_at"):
            self.assertIn(key, p)
        self.assertEqual(p["prompt_id"], "crm-intent-resolver@1.0.0")
        self.assertEqual(set(p["schema_hashes"]),
                         {"scope-decision", "intent-decision", "intent-resolution"})


class ShadowModeTest(unittest.IsolatedAsyncioTestCase):
    """Shadow must be observationally identical to deterministic."""

    async def asyncSetUp(self):
        base = build_runtime().settings
        self.shadow_settings = type(base)(**{**vars(base), "intent_mode": MODE_SHADOW})
        self.det_settings = type(base)(**{**vars(base), "intent_mode": "deterministic"})

    def _runtime(self, settings):
        runtime = build_runtime(settings)
        tool = _FakeTool()
        runtime.tools._tools["engine.renewals.needs_attention"] = tool
        return runtime, tool

    async def test_shadow_result_never_selects_the_route(self):
        """The resolver says redirect; the guard says allow. Production follows the guard."""
        runtime, tool = self._runtime(self.shadow_settings)
        provider = ScriptedProvider().script_default(
            resolution(decision="redirect", domain=None, actions=(),
                       scope_decision="redirect", scope="non_crm",
                       reason="out_of_scope", requires=False)
        )
        dispatcher = MessageDispatcher(runtime, resolver=_resolver(provider))
        await dispatcher.dispatch(text="which renewals need attention?", thread_id=None,
                                  user_token="jwt", owner_user_id="uw-1")
        # The guard routed, so the engine was called despite the shadow redirect.
        self.assertEqual(len(tool.calls), 1)

    async def test_shadow_adds_no_user_visible_model_prose(self):
        shadow_rt, _ = self._runtime(self.shadow_settings)
        det_rt, _ = self._runtime(self.det_settings)
        provider = ScriptedProvider().script_default(resolution())

        shadow_envelope = await MessageDispatcher(
            shadow_rt, resolver=_resolver(provider)
        ).dispatch(text="tell me a joke", thread_id=None, user_token="jwt", owner_user_id="uw-1")
        det_envelope = await MessageDispatcher(
            det_rt, resolver=_resolver(ScriptedProvider())
        ).dispatch(text="tell me a joke", thread_id=None, user_token="jwt", owner_user_id="uw-1")

        # Byte-for-byte identical user-visible parts.
        self.assertEqual(
            [p.get("text") for p in shadow_envelope["parts"]],
            [p.get("text") for p in det_envelope["parts"]],
        )

    async def test_shadow_adds_no_extra_engine_call(self):
        runtime, tool = self._runtime(self.shadow_settings)
        provider = ScriptedProvider().script_default(resolution())
        await MessageDispatcher(runtime, resolver=_resolver(provider)).dispatch(
            text="which renewals need attention?", thread_id=None,
            user_token="jwt", owner_user_id="uw-1")
        self.assertEqual(len(tool.calls), 1)

    async def test_disagreement_is_recorded_for_inspection(self):
        runtime, _ = self._runtime(self.shadow_settings)
        provider = ScriptedProvider().script_default(
            resolution(decision="redirect", domain=None, actions=(),
                       scope_decision="redirect", scope="non_crm",
                       reason="out_of_scope", requires=False)
        )
        await MessageDispatcher(runtime, resolver=_resolver(provider)).dispatch(
            text="which renewals need attention?", thread_id=None,
            user_token="jwt", owner_user_id="uw-1")
        digests = [
            c.request_digest for c in runtime.repository._tool_calls.values()
            if c.tool_name == "intent.shadow_compare"
        ]
        self.assertTrue(digests)
        self.assertIn("agree=False", digests[0])

    async def test_shadow_never_breaks_a_turn_when_the_resolver_fails(self):
        """A model outage during shadow evaluation is a data gap, not an incident."""
        runtime, tool = self._runtime(self.shadow_settings)
        provider = ScriptedProvider()  # unscripted → raises on use
        envelope = await MessageDispatcher(runtime, resolver=_resolver(provider)).dispatch(
            text="which renewals need attention?", thread_id=None,
            user_token="jwt", owner_user_id="uw-1")
        self.assertTrue(envelope["parts"])
        self.assertEqual(len(tool.calls), 1)

    async def test_shadow_digest_carries_no_user_text(self):
        runtime, _ = self._runtime(self.shadow_settings)
        provider = ScriptedProvider().script_default(resolution())
        secret = "renewals for policy 12345 Jane Doe"
        await MessageDispatcher(runtime, resolver=_resolver(provider)).dispatch(
            text=secret, thread_id=None, user_token="jwt", owner_user_id="uw-1")
        for call in runtime.repository._tool_calls.values():
            self.assertNotIn("Jane", call.request_digest)
            self.assertNotIn("12345", call.request_digest)


class RolloutDefaultTest(unittest.TestCase):
    def test_direct_routing_is_not_the_default_while_the_gate_is_red(self):
        """Spec §33: direct routing is enabled only after the §30.4 gates pass. The
        2026-07-25 run is red, so the shipped default must not be `direct`."""
        from app.config import load_settings

        self.assertNotEqual(load_settings().intent_mode, "direct")


class LoadTest(unittest.IsolatedAsyncioTestCase):
    """Concurrency behaviour at 1, 2, and 4 (spec §30 load profile).

    Run against the scripted provider so this is a *concurrency* test, not a GPU
    benchmark: it proves the resolver has no shared-state contention and that latency
    does not blow up with parallel requests.
    """

    async def _measure(self, concurrency: int) -> tuple[float, int]:
        provider = ScriptedProvider().script_default(resolution())
        resolver = _resolver(provider)
        started = time.monotonic()
        outcomes = await asyncio.gather(
            *(resolver.resolve("show me my renewals") for _ in range(concurrency))
        )
        elapsed = (time.monotonic() - started) * 1000
        return elapsed, sum(1 for o in outcomes if o.should_route)

    async def test_concurrency_1_2_4_all_succeed(self):
        for concurrency in (1, 2, 4):
            elapsed, routed = await self._measure(concurrency)
            self.assertEqual(routed, concurrency, f"at concurrency {concurrency}")
            self.assertLess(elapsed, 5000, f"at concurrency {concurrency}")

    async def test_no_cross_request_state_leaks_at_concurrency_4(self):
        provider = ScriptedProvider().script_default(resolution())
        resolver = _resolver(provider)
        texts = ["show me my renewals", "list renewals", "renewals please", "my renewals"]
        outcomes = await asyncio.gather(*(resolver.resolve(t) for t in texts))
        self.assertTrue(all(o.should_route for o in outcomes))
        self.assertEqual(len(provider.calls), 4)


class _FakeTool:
    def __init__(self):
        self.calls = []

    async def invoke(self, **kwargs):
        self.calls.append(kwargs)
        return {"data": [{
            "renewal_id": "r-1", "account_name": "Acme Mfg", "days_to_expiry": 12,
            "workflow_state": "Identified", "no_contact_flag": True,
            "broker_name": "Atlas", "can_draft_outreach": True,
        }]}


def _expected_for(prompt: str) -> dict:
    for case in load_dataset("direct", EVAL_ROOT):
        if case["text"] == prompt:
            return case
    return {"domain": "renewals", "actions": ["renewals.list_attention"]}


async def _as_result(payload):
    from app.models.router import ModelProvenance, StructuredModelResult, content_hash
    import json

    text = json.dumps(payload, sort_keys=True)
    return StructuredModelResult(
        data=payload,
        provenance=ModelProvenance(model="scripted-1", content_hash=content_hash(text)),
    )


if __name__ == "__main__":
    unittest.main()
