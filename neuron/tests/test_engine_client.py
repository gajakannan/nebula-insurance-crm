import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.engine_client import EngineClient, EngineResponse
from app.errors import UpstreamAuthError, UpstreamUnavailableError


class RecordingSender:
    """Fake transport that records the outgoing request and returns a canned response."""

    def __init__(self, response=None, raise_exc=None):
        self.response = response
        self.raise_exc = raise_exc
        self.calls = []

    async def __call__(self, *, method, url, headers, json, params):
        self.calls.append({"method": method, "url": url, "headers": headers, "json": json, "params": params})
        if self.raise_exc is not None:
            raise self.raise_exc
        return self.response


class EngineClientTest(unittest.IsolatedAsyncioTestCase):
    async def test_forwards_bearer_token(self):
        sender = RecordingSender(response=EngineResponse(200, {"ok": True}))
        client = EngineClient("http://engine", sender=sender)
        body = await client.call("GET", "/renewals/needs-attention", user_token="tok-123")
        self.assertEqual(body, {"ok": True})
        self.assertEqual(sender.calls[0]["headers"]["Authorization"], "Bearer tok-123")
        self.assertEqual(sender.calls[0]["url"], "http://engine/renewals/needs-attention")

    async def test_connection_error_becomes_upstream_unavailable(self):
        sender = RecordingSender(raise_exc=ConnectionError("refused"))
        client = EngineClient("http://engine", sender=sender)
        with self.assertRaises(UpstreamUnavailableError):
            await client.call("GET", "/renewals/needs-attention", user_token="tok")

    async def test_timeout_becomes_upstream_unavailable(self):
        sender = RecordingSender(raise_exc=TimeoutError())
        client = EngineClient("http://engine", sender=sender)
        with self.assertRaises(UpstreamUnavailableError):
            await client.call("GET", "/x", user_token="tok")

    async def test_forbidden_becomes_typed_auth_error(self):
        sender = RecordingSender(response=EngineResponse(403, {"title": "denied"}))
        client = EngineClient("http://engine", sender=sender)
        with self.assertRaises(UpstreamAuthError) as ctx:
            await client.call("POST", "/renewals/1/outreach-draft", user_token="tok")
        self.assertEqual(ctx.exception.status, 403)

    async def test_server_error_becomes_upstream_unavailable(self):
        sender = RecordingSender(response=EngineResponse(500, None))
        client = EngineClient("http://engine", sender=sender)
        with self.assertRaises(UpstreamUnavailableError):
            await client.call("GET", "/x", user_token="tok")


if __name__ == "__main__":
    unittest.main()
