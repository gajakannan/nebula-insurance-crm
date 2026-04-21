#!/bin/sh
# deploy-prod.sh -- Deploy to the production environment.
#
# Safety:
# - Requires explicit confirmation:
#   --confirm-production
#   or CONFIRM_PRODUCTION_DEPLOY=yes
#
# Usage:
#   sh scripts/deploy-prod.sh --confirm-production [options]
#
# See:
#   sh scripts/deploy.sh --help

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec "$SCRIPT_DIR/deploy.sh" prod "$@"
