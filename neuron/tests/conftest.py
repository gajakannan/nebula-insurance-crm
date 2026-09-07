"""Put the neuron/ root on sys.path so ``import app`` / ``import crm_agents`` resolve
under both pytest and ``python -m unittest`` without an installed package."""

import sys
import os
from pathlib import Path

NEURON_ROOT = Path(__file__).resolve().parents[1]
if str(NEURON_ROOT) not in sys.path:
    sys.path.insert(0, str(NEURON_ROOT))

# HTTP unit tests use opaque fixture tokens and do not run an engine. The deployed
# default is engine verification; this explicit setting keeps the test shortcut
# visible and impossible to select accidentally in production.
os.environ.setdefault("NEURON_AUTH_MODE", "unverified-test")
