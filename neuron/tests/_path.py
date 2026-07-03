"""sys.path shim imported by each test module (works under unittest, which does not
load conftest.py). Kept tiny and idempotent."""

import sys
from pathlib import Path

NEURON_ROOT = Path(__file__).resolve().parents[1]
if str(NEURON_ROOT) not in sys.path:
    sys.path.insert(0, str(NEURON_ROOT))
