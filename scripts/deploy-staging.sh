#!/bin/sh
# deploy-staging.sh -- Deploy to the staging environment.
#
# Usage:
#   sh scripts/deploy-staging.sh [options]
#
# See:
#   sh scripts/deploy.sh --help

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec "$SCRIPT_DIR/deploy.sh" staging "$@"
