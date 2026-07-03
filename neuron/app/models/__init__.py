"""Model router + providers.

F0038 runs on a deterministic **mock** provider (no live Anthropic calls) selected
by config. The real provider client is left injectable behind the router seam for a
later live smoke test (assembly plan, ADR-027 §model-router).
"""
