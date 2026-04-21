#!/bin/sh
# rollback.sh -- Roll back a deployment.
#
# Usage:
#   sh scripts/rollback.sh --environment ENV [options]
#
# Options:
#   --environment ENV   dev|staging|prod (required)
#   --strategy MODE     auto|custom|k8s (default: auto)
#   --namespace NAME    Kubernetes namespace (default: environment name)
#   --resource NAME     Deployment name (repeatable)
#   --selector LABEL    Label selector used to discover deployments
#   --to-revision N     Specific rollout revision
#   --dry-run           Print intended rollback actions only
#   -h, --help          Show help
#
# Environment overrides:
#   ROLLBACK_CMD
#   ROLLBACK_DEV_CMD
#   ROLLBACK_STAGING_CMD
#   ROLLBACK_PROD_CMD
#
# Exit codes:
#   0  Rollback succeeded
#   1  Rollback command failed
#   2  Setup/usage/tooling error

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
ENVIRONMENT="${ROLLBACK_ENV:-}"
STRATEGY="${ROLLBACK_STRATEGY:-auto}"
NAMESPACE="${ROLLBACK_NAMESPACE:-}"
SELECTOR="${ROLLBACK_SELECTOR:-}"
TO_REVISION="${ROLLBACK_TO_REVISION:-}"
RESOURCES=""
DRY_RUN=0

print_usage() {
  cat <<EOF
Usage: $0 --environment ENV [options]

Options:
  --environment ENV   dev|staging|prod (required)
  --strategy MODE     auto|custom|k8s (default: auto)
  --namespace NAME    Kubernetes namespace (default: environment name)
  --resource NAME     Deployment name (repeatable)
  --selector LABEL    Label selector used to discover deployments
  --to-revision N     Specific rollout revision
  --dry-run           Print intended rollback actions only
  -h, --help          Show help
EOF
}

resolve_custom_cmd() {
  case "$ENVIRONMENT" in
    dev)
      if [ -n "${ROLLBACK_DEV_CMD:-}" ]; then
        echo "$ROLLBACK_DEV_CMD"
      else
        echo "${ROLLBACK_CMD:-}"
      fi
      ;;
    staging|stage)
      if [ -n "${ROLLBACK_STAGING_CMD:-}" ]; then
        echo "$ROLLBACK_STAGING_CMD"
      else
        echo "${ROLLBACK_CMD:-}"
      fi
      ;;
    prod|production)
      if [ -n "${ROLLBACK_PROD_CMD:-}" ]; then
        echo "$ROLLBACK_PROD_CMD"
      else
        echo "${ROLLBACK_CMD:-}"
      fi
      ;;
    *)
      echo "${ROLLBACK_CMD:-}"
      ;;
  esac
}

kubectl_run() {
  if [ -n "$NAMESPACE" ]; then
    kubectl -n "$NAMESPACE" "$@"
  else
    kubectl "$@"
  fi
}

run_custom_rollback() {
  custom_cmd="$1"
  if [ -z "$custom_cmd" ]; then
    echo "ERROR: no custom rollback command configured." >&2
    exit 2
  fi

  echo "Running custom rollback for '${ENVIRONMENT}'"
  (
    cd "$ROOT_DIR" || exit 2
    ROLLBACK_ENV="$ENVIRONMENT" ROLLBACK_NAMESPACE="$NAMESPACE" sh -c "$custom_cmd"
  )
  return $?
}

append_resource() {
  name="$1"
  if [ -z "$name" ]; then
    return 0
  fi
  if echo "$name" | grep -q '/'; then
    RESOURCES="${RESOURCES}
${name}"
  else
    RESOURCES="${RESOURCES}
deployment/${name}"
  fi
}

run_k8s_rollback() {
  if ! command -v kubectl >/dev/null 2>&1; then
    echo "ERROR: kubectl is not available." >&2
    exit 2
  fi

  if [ -z "$NAMESPACE" ]; then
    NAMESPACE="$ENVIRONMENT"
  fi

  targets="$RESOURCES"
  if [ -z "$(echo "$targets" | tr -d '[:space:]')" ] && [ -n "$SELECTOR" ]; then
    discovered=$(kubectl_run get deployments -l "$SELECTOR" -o name)
    if [ $? -ne 0 ]; then
      return 1
    fi
    targets="$discovered"
  fi

  if [ -z "$(echo "$targets" | tr -d '[:space:]')" ]; then
    echo "ERROR: no rollback targets found." >&2
    echo "Use --resource NAME and/or --selector LABEL." >&2
    exit 2
  fi

  echo "Running Kubernetes rollback in namespace '${NAMESPACE}'"

  failed=0
  OLD_IFS=$IFS
  IFS='
'
  for target in $targets; do
    [ -z "$target" ] && continue
    if [ "$DRY_RUN" -eq 1 ]; then
      if [ -n "$TO_REVISION" ]; then
        echo "Dry run: kubectl -n ${NAMESPACE} rollout undo ${target} --to-revision=${TO_REVISION}"
      else
        echo "Dry run: kubectl -n ${NAMESPACE} rollout undo ${target}"
      fi
      continue
    fi

    if [ -n "$TO_REVISION" ]; then
      kubectl_run rollout undo "$target" --to-revision="$TO_REVISION" || failed=1
    else
      kubectl_run rollout undo "$target" || failed=1
    fi
  done
  IFS=$OLD_IFS

  return $failed
}

while [ $# -gt 0 ]; do
  case "$1" in
    --environment)
      ENVIRONMENT="$2"
      shift 2
      ;;
    --strategy)
      STRATEGY="$2"
      shift 2
      ;;
    --namespace)
      NAMESPACE="$2"
      shift 2
      ;;
    --resource)
      append_resource "$2"
      shift 2
      ;;
    --selector)
      SELECTOR="$2"
      shift 2
      ;;
    --to-revision)
      TO_REVISION="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      print_usage
      exit 0
      ;;
    *)
      echo "ERROR: unknown option: $1" >&2
      print_usage >&2
      exit 2
      ;;
  esac
done

if [ -z "$ENVIRONMENT" ]; then
  echo "ERROR: --environment is required." >&2
  exit 2
fi

case "$ENVIRONMENT" in
  dev|staging|stage|prod|production) ;;
  *)
    echo "ERROR: invalid environment '$ENVIRONMENT'." >&2
    exit 2
    ;;
esac

custom_cmd=$(resolve_custom_cmd)

if [ "$STRATEGY" = "custom" ]; then
  run_custom_rollback "$custom_cmd"
  rc=$?
elif [ "$STRATEGY" = "k8s" ]; then
  run_k8s_rollback
  rc=$?
elif [ "$STRATEGY" = "auto" ]; then
  if [ -n "$custom_cmd" ]; then
    run_custom_rollback "$custom_cmd"
    rc=$?
  elif command -v kubectl >/dev/null 2>&1; then
    run_k8s_rollback
    rc=$?
  else
    echo "ERROR: could not determine rollback strategy." >&2
    echo "Set ROLLBACK_*_CMD or install/configure kubectl targets." >&2
    exit 2
  fi
else
  echo "ERROR: invalid strategy '$STRATEGY'." >&2
  exit 2
fi

if [ $rc -ne 0 ]; then
  echo "Rollback failed for '${ENVIRONMENT}' (exit ${rc})." >&2
  exit 1
fi

if [ "$DRY_RUN" -eq 1 ]; then
  echo "Rollback dry run completed for '${ENVIRONMENT}'."
else
  echo "Rollback completed for '${ENVIRONMENT}'."
fi

exit 0
