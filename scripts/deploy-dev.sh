#!/bin/sh
# deploy-dev.sh -- Deploy to the development environment.
#
# Usage:
#   sh scripts/deploy-dev.sh [options]
#
# See:
#   sh scripts/deploy.sh --help

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec "$SCRIPT_DIR/deploy.sh" dev "$@"
