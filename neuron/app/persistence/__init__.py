"""Neuron operation-store persistence (ADR-028 §1).

The ``neuron.*`` schema (six tables) is Neuron-owned and written directly — never
via an engine pass-through. F0038 runs on an in-memory implementation behind the
``NeuronRepository`` interface; the durable Postgres home is scaffolded in
``migrations/`` and swapped in without reshaping callers.
"""
