"""Put the neuron/ root on sys.path so ``import app`` / ``import crm_agents`` resolve
under both pytest and ``python -m unittest`` without an installed package."""

import sys
from pathlib import Path

NEURON_ROOT = Path(__file__).resolve().parents[1]
if str(NEURON_ROOT) not in sys.path:
    sys.path.insert(0, str(NEURON_ROOT))
