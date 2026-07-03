"""Neuron Companion runtime (F0038).

A stateless AI companion service for Nebula CRM. The .NET engine remains the CRM
source of truth and authorization boundary; Neuron owns only its ``neuron.*``
operation store (threads, messages, agent runs, tool calls, provenance) and calls
the engine **as the user** (forwarded authentik token). Governed by ADR-027
(A2A-aligned internal orchestration) and ADR-028 (persistence + outreach authz).
"""

__version__ = "0.1.0"
