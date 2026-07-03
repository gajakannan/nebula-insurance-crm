"""Resolve the authenticated subject from the forwarded token for owner-scoping.

Neuron does **not** verify the token — the engine is the verifier/authorizer. Neuron
only needs a stable subject id to owner-scope its threads (ADR-028 §1). This reads the
JWT payload segment WITHOUT signature verification; an opaque/unparseable token falls
back to a stable pseudonymous digest (no PII, no raw token stored).
"""

from __future__ import annotations

import base64
import hashlib
import json


def _decode_claims(token: str) -> dict:
    """Best-effort UNVERIFIED decode of the JWT payload segment (engine is the verifier)."""
    parts = token.split(".")
    if len(parts) >= 2:
        try:
            payload_b64 = parts[1]
            padding = "=" * (-len(payload_b64) % 4)
            claims = json.loads(base64.urlsafe_b64decode(payload_b64 + padding))
            if isinstance(claims, dict):
                return claims
        except (ValueError, TypeError):
            pass  # not a JWT / undecodable payload → no claims
    return {}


def subject_from_token(token: str) -> str:
    claims = _decode_claims(token)
    for key in ("sub", "preferred_username", "email", "uid"):
        value = claims.get(key)
        if value:
            return str(value)
    return "anon:" + hashlib.sha256(token.encode("utf-8")).hexdigest()[:16]


def persona_from_token(token: str) -> str:
    """Best-effort renewal-owner persona for DAU telemetry (F0038-S0008).

    Telemetry-only and NON-authoritative — the engine remains the sole authorizer. Reads
    role/group claims from the UNVERIFIED token; defaults to 'underwriter' (the primary
    renewal-owner persona per the story) when no distribution role is present.
    """
    claims = _decode_claims(token)
    roles: list[str] = []
    for key in ("roles", "groups"):
        value = claims.get(key)
        if isinstance(value, list):
            roles.extend(str(v) for v in value)
        elif isinstance(value, str):
            roles.append(value)
    realm = claims.get("realm_access")
    if isinstance(realm, dict) and isinstance(realm.get("roles"), list):
        roles.extend(str(v) for v in realm["roles"])
    return "distribution" if "distribution" in " ".join(roles).lower() else "underwriter"
