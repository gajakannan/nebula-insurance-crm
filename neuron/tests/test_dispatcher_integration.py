"""F0039-S0007 — dispatcher, persistence, and provenance integration.

The guarantees under test are all about *ordering* and *what is recorded*:

* the inbound message is persisted **before** anything resolves or routes;
* a persistence failure stops the turn before the model and the engine;
* the resolver runs **before** any head dispatch, and a head is reached only after a
  validated route;
* every outcome — routed, redirect, clarify, resolver failure — is persisted as a
  replayable envelope;
* provenance is traceable and content-free.
"""

from __future__ import annotations

import os
import sys
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.bootstrap import build_runtime
from app.errors import PersistenceUnavailableError
from app.intent.catalog import load_catalog
from app.intent.prompt_registry import INTENT_RESOLVER_PROMPT, PromptRegistry
from app.intent.resolver import IntentResolver
from app.messages import MessageDispatcher
from app.models.errors import ProviderTimeoutError
from app.models.router import ModelRouter
from app.models.scripted_provider import ScriptedProvider

NEURON_ROOT = Path(__file__).resolve().parents[1]
HEADS = {"crm.renewals.head", "crm.tasks.head", "crm.pipeline.head", "crm.broker_activity.head"}
CATALOG = load_catalog(NEURON_ROOT / "config" / "intent-catalog.yaml", registered_head_ids=HEADS)
PROMPT = PromptRegistry(NEURON_ROOT / "prompts").load(INTENT_RESOLVER_PROMPT)

OWNER = "uw-1"
TOKEN = "jwt.tok"


def payload(scope_overrides=None, intent_overrides=None):
    scope = {
        "schema_version": "1.0.0", "decision": "allow", "scope": "crm",
        "reason_code": "in_scope", "requires_intent_resolution": True,
        "clarification_code": None,
    }
    intent = {
        "schema_version": "1.0.0", "decision": "route", "domain": "renewals",
        "actions": ["renewals.list_attention"], "entities": {},
        "needs_context": False, "needs_adjudication": False, "clarification_code": None,
    }
    scope.update(scope_overrides or {})
    intent.update(intent_overrides or {})
    return {"schema_version": "1.0.0", "scope": scope, "intent": intent}


class FakeTool:
    """Stands in for the engine call, and records whether it was reached at all."""

    def __init__(self, items=None):
        self.calls = []
        # Same shape the F0038 head fixtures use — the head maps snake_case engine rows.
        self._items = items if items is not None else [
            {
                "renewal_id": "r-1",
                "account_name": "Acme Mfg",
                "days_to_expiry": 12,
                "workflow_state": "Identified",
                "no_contact_flag": True,
                "broker_name": "Atlas Brokerage",
                "can_draft_outreach": True,
            }
        ]

    async def invoke(self, **kwargs):
        self.calls.append(kwargs)
        return {"data": self._items}


class FakeBrokerTool:
    def __init__(self, items=None):
        self.calls = []
        self._items = items or []

    async def invoke(self, **kwargs):
        self.calls.append(kwargs)
        return {"data": self._items}


class DispatcherTestBase(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        # Pin direct mode: the shipped default is `shadow` until the §30.4 gates go
        # green (S0008), and these tests exercise the direct path specifically.
        base = build_runtime().settings
        self.rt = build_runtime(type(base)(**{**vars(base), "intent_mode": "direct"}))
        self.tool = FakeTool()
        self.rt.tools._tools["engine.renewals.needs_attention"] = self.tool
        self.broker_tool = FakeBrokerTool()
        self.rt.tools._tools["engine.timeline.list_broker_activity"] = self.broker_tool

    def _dispatcher(self, provider):
        resolver = IntentResolver(
            model_router=ModelRouter({"scripted": provider}, default="scripted"),
            catalog=CATALOG,
            prompt=PROMPT,
        )
        return MessageDispatcher(self.rt, resolver=resolver)

    async def _messages(self, thread_id):
        return await self.rt.repository.get_messages(thread_id, OWNER)

    async def _only_thread_id(self):
        page = await self.rt.repository.list_threads(OWNER)
        return page.items[0].thread_id if hasattr(page.items[0], "thread_id") else page.items[0].id


class PersistFirstTest(DispatcherTestBase):
    async def test_user_message_is_persisted_before_resolution(self):
        provider = ScriptedProvider().script_default(payload())
        await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        thread_id = await self._only_thread_id()
        history = await self._messages(thread_id)
        self.assertEqual(history[0].role, "user")
        self.assertEqual(history[0].sequence, 1)

    async def test_persistence_failure_stops_before_the_model_and_the_engine(self):
        """The whole point of persist-first: no work is done for a turn we cannot record."""
        provider = ScriptedProvider().script_default(payload())
        dispatcher = self._dispatcher(provider)

        async def failing_add_message(*args, **kwargs):
            raise PersistenceUnavailableError("store down")

        self.rt.repository.add_message = failing_add_message

        with self.assertRaises(PersistenceUnavailableError):
            await dispatcher.dispatch(
                text="show me my renewals", thread_id=None,
                user_token=TOKEN, owner_user_id=OWNER,
            )
        self.assertEqual(provider.calls, [])  # model never called
        self.assertEqual(self.tool.calls, [])  # engine never called

    async def test_duplicate_client_key_does_not_duplicate_the_turn(self):
        provider = ScriptedProvider().script_default(payload())
        dispatcher = self._dispatcher(provider)
        thread = await self.rt.repository.create_thread(OWNER, title="t")
        for _ in range(2):
            await dispatcher.dispatch(
                text="show me my renewals", thread_id=thread.id,
                user_token=TOKEN, owner_user_id=OWNER, client_message_key="dup-1",
            )
        user_turns = [m for m in await self._messages(thread.id) if m.role == "user"]
        self.assertEqual(len(user_turns), 1)


class ResolveBeforeDispatchTest(DispatcherTestBase):
    async def test_validated_route_reaches_the_head_and_the_engine(self):
        provider = ScriptedProvider().script_default(payload())
        envelope = await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        self.assertEqual(len(provider.calls), 1)
        self.assertEqual(len(self.tool.calls), 1)
        self.assertTrue(any(p["part_type"] == "app" for p in envelope["parts"]))

    async def test_broker_route_reaches_only_the_fixed_read(self):
        provider = ScriptedProvider().script_default(
            payload(intent_overrides={
                "domain": "broker_activity",
                "actions": ["broker_activity.list"],
            })
        )
        envelope = await self._dispatcher(provider).dispatch(
            text="show recent broker activity", thread_id=None,
            user_token=TOKEN, owner_user_id=OWNER,
        )
        self.assertEqual(len(self.broker_tool.calls), 1)
        self.assertEqual(self.broker_tool.calls[0]["params"], {
            "entityType": "Broker", "page": 1, "pageSize": 20, "internalOnly": True,
        })
        self.assertEqual(envelope["parts"][0]["text"], "No recent broker activity.")

    async def test_broker_filter_and_write_proposals_make_no_engine_call(self):
        cases = (
            ({"domain": "broker_activity", "actions": ["broker_activity.list"],
              "entities": {"broker_name": "Atlas"}}, "filters aren't supported"),
            ({"domain": "broker_activity", "actions": ["broker_activity.delete"]},
             "read-only"),
        )
        for intent_overrides, expected_copy in cases:
            with self.subTest(intent=intent_overrides):
                provider = ScriptedProvider().script_default(
                    payload(intent_overrides=intent_overrides)
                )
                before = len(self.broker_tool.calls)
                envelope = await self._dispatcher(provider).dispatch(
                    text="broker request", thread_id=None,
                    user_token=TOKEN, owner_user_id=OWNER,
                )
                self.assertEqual(len(self.broker_tool.calls), before)
                self.assertIn(expected_copy, envelope["parts"][0]["text"])

    async def test_redirect_makes_no_engine_call(self):
        provider = ScriptedProvider().script_default(
            payload(
                scope_overrides={
                    "decision": "redirect", "scope": "non_crm",
                    "reason_code": "out_of_scope", "requires_intent_resolution": False,
                },
                intent_overrides={"decision": "redirect", "domain": None, "actions": []},
            )
        )
        envelope = await self._dispatcher(provider).dispatch(
            text="what is the capital of France", thread_id=None,
            user_token=TOKEN, owner_user_id=OWNER,
        )
        self.assertEqual(self.tool.calls, [])
        self.assertTrue(all(p["part_type"] == "text" for p in envelope["parts"]))

    async def test_invented_action_makes_no_engine_call(self):
        provider = ScriptedProvider().script_default(
            payload(intent_overrides={"actions": ["show_renewals_needing_attention"]})
        )
        await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        self.assertEqual(self.tool.calls, [])

    async def test_resolver_timeout_makes_no_engine_call(self):
        provider = ScriptedProvider()
        provider.script_error("show me my renewals", ProviderTimeoutError("timeout"))
        envelope = await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        self.assertEqual(self.tool.calls, [])
        self.assertTrue(envelope["parts"])

    async def test_write_like_action_asks_for_confirmation_instead_of_executing(self):
        provider = ScriptedProvider().script_default(
            payload(
                intent_overrides={
                    "actions": ["renewals.mock_send"], "entities": {"renewal_id": "R-1"},
                }
            )
        )
        envelope = await self._dispatcher(provider).dispatch(
            text="send the outreach for R-1", thread_id=None,
            user_token=TOKEN, owner_user_id=OWNER,
        )
        # Proposed, not committed: no engine call until the user confirms.
        self.assertEqual(self.tool.calls, [])
        self.assertIn("confirm", envelope["parts"][0]["text"].casefold())


class EveryOutcomeIsPersistedTest(DispatcherTestBase):
    async def _dispatch_and_read(self, provider, text="show me my renewals"):
        thread = await self.rt.repository.create_thread(OWNER, title="t")
        await self._dispatcher(provider).dispatch(
            text=text, thread_id=thread.id, user_token=TOKEN, owner_user_id=OWNER
        )
        return await self._messages(thread.id)

    async def test_routed_turn_persists_both_messages(self):
        history = await self._dispatch_and_read(ScriptedProvider().script_default(payload()))
        self.assertEqual([m.role for m in history], ["user", "assistant"])

    async def test_redirect_turn_persists_both_messages(self):
        provider = ScriptedProvider().script_default(
            payload(
                scope_overrides={
                    "decision": "redirect", "scope": "non_crm",
                    "reason_code": "out_of_scope", "requires_intent_resolution": False,
                },
                intent_overrides={"decision": "redirect", "domain": None, "actions": []},
            )
        )
        history = await self._dispatch_and_read(provider)
        self.assertEqual([m.role for m in history], ["user", "assistant"])

    async def test_clarify_turn_persists_both_messages(self):
        provider = ScriptedProvider().script_default(
            payload(intent_overrides={"actions": ["renewals.view"]})
        )
        history = await self._dispatch_and_read(provider)
        self.assertEqual([m.role for m in history], ["user", "assistant"])

    async def test_resolver_failure_turn_persists_both_messages(self):
        provider = ScriptedProvider()
        provider.script_error("show me my renewals", ProviderTimeoutError("timeout"))
        history = await self._dispatch_and_read(provider)
        self.assertEqual([m.role for m in history], ["user", "assistant"])

    async def test_turn_replays_in_server_sequence_order(self):
        history = await self._dispatch_and_read(ScriptedProvider().script_default(payload()))
        self.assertEqual([m.sequence for m in history], [1, 2])


class ProvenanceTest(DispatcherTestBase):
    async def _runs(self):
        return list(self.rt.repository._runs.values())

    async def _tool_calls(self):
        return list(self.rt.repository._tool_calls.values())

    async def test_resolver_run_and_head_run_are_both_recorded(self):
        provider = ScriptedProvider().script_default(payload())
        await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        card_ids = {run.card_id for run in await self._runs()}
        self.assertIn("crm.intent_resolver", card_ids)
        self.assertIn("crm.renewals.head", card_ids)

    async def test_resolution_digest_carries_codes_not_user_text(self):
        secret = "policy 12345 for Jane Doe"
        provider = ScriptedProvider().script_default(payload())
        await self._dispatcher(provider).dispatch(
            text=secret, thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        digests = [c.request_digest for c in await self._tool_calls() if c.tool_name == "intent.resolve"]
        self.assertTrue(digests)
        for digest in digests:
            self.assertNotIn("Jane", digest)
            self.assertNotIn("12345", digest)
            self.assertIn("scope=", digest)

    async def test_model_and_prompt_provenance_is_recorded(self):
        provider = ScriptedProvider().script_default(payload())
        await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        resolver_runs = [r for r in await self._runs() if r.card_id == "crm.intent_resolver"]
        events = await self.rt.repository.list_provenance(resolver_runs[0].id)
        self.assertEqual(len(events), 1)
        self.assertEqual(events[0].prompt_id, "crm-intent-resolver@1.0.0")
        self.assertTrue(events[0].content_hash.startswith("sha256:"))

    async def test_provenance_shape_cannot_hold_raw_content(self):
        provider = ScriptedProvider().script_default(payload())
        await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        resolver_runs = [r for r in await self._runs() if r.card_id == "crm.intent_resolver"]
        events = await self.rt.repository.list_provenance(resolver_runs[0].id)
        forbidden = {"prompt", "raw_prompt", "response", "raw_response", "text", "message"}
        self.assertFalse(forbidden & set(vars(events[0])))

    async def test_clarify_records_input_required(self):
        provider = ScriptedProvider().script_default(
            payload(intent_overrides={"actions": ["renewals.view"]})
        )
        await self._dispatcher(provider).dispatch(
            text="show me that renewal", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        resolver_runs = [r for r in await self._runs() if r.card_id == "crm.intent_resolver"]
        self.assertEqual(resolver_runs[0].state, "input_required")


class RollbackModeTest(DispatcherTestBase):
    async def test_deterministic_mode_never_calls_the_model(self):
        """The rollback path must not depend on the model being reachable at all."""
        settings = self.rt.settings
        deterministic = type(settings)(**{**vars(settings), "intent_mode": "deterministic"})
        runtime = build_runtime(deterministic)
        tool = FakeTool()
        runtime.tools._tools["engine.renewals.needs_attention"] = tool

        provider = ScriptedProvider()  # unscripted: any call would raise
        resolver = IntentResolver(
            model_router=ModelRouter({"scripted": provider}, default="scripted"),
            catalog=CATALOG, prompt=PROMPT,
        )
        dispatcher = MessageDispatcher(runtime, resolver=resolver)

        await dispatcher.dispatch(
            text="which renewals need attention?", thread_id=None,
            user_token=TOKEN, owner_user_id=OWNER,
        )
        self.assertEqual(provider.calls, [])
        self.assertEqual(len(tool.calls), 1)  # still routes via the keyword guard


if __name__ == "__main__":
    unittest.main()


class AssistantPersistenceFailureTest(DispatcherTestBase):
    """G3 finding M1: an unsaved reply must not look saved."""

    async def _dispatch_with_failing_assistant_write(self):
        provider = ScriptedProvider().script_default(payload())
        dispatcher = self._dispatcher(provider)
        thread = await self.rt.repository.create_thread(OWNER, title="t")
        real_add = self.rt.repository.add_message
        calls = {"n": 0}

        async def add_message(*args, **kwargs):
            calls["n"] += 1
            if kwargs.get("role") == "assistant":
                raise PersistenceUnavailableError("store down")
            return await real_add(*args, **kwargs)

        self.rt.repository.add_message = add_message
        envelope = await dispatcher.dispatch(
            text="show me my renewals", thread_id=thread.id,
            user_token=TOKEN, owner_user_id=OWNER,
        )
        self.rt.repository.add_message = real_add
        return envelope, thread

    async def test_unsaved_reply_is_marked_failed(self):
        envelope, _ = await self._dispatch_with_failing_assistant_write()
        statuses = [p for p in envelope["parts"] if p["part_type"] == "status"]
        self.assertTrue(statuses, "expected a status part marking the reply unsaved")
        self.assertEqual(statuses[-1]["state"], "failed")
        self.assertIn("could not be saved", statuses[-1]["detail"])

    async def test_the_reply_content_is_still_returned(self):
        """Losing the answer as well as the record would help nobody."""
        envelope, _ = await self._dispatch_with_failing_assistant_write()
        self.assertTrue([p for p in envelope["parts"] if p["part_type"] != "status"])

    async def test_a_successful_turn_carries_no_failure_marker(self):
        provider = ScriptedProvider().script_default(payload())
        envelope = await self._dispatcher(provider).dispatch(
            text="show me my renewals", thread_id=None, user_token=TOKEN, owner_user_id=OWNER
        )
        failed = [p for p in envelope["parts"]
                  if p["part_type"] == "status" and p.get("state") == "failed"]
        self.assertEqual(failed, [])
