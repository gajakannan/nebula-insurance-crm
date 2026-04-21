#!/bin/sh
# deploy.sh -- Generic deployment runner for dev/staging/prod.
#
# Wrapper scripts:
#   scripts/deploy-dev.sh
#   scripts/deploy-staging.sh
#   scripts/deploy-prod.sh
#
# Usage:
#   sh scripts/deploy.sh <environment> [options]
#
# Options:
#   --strategy STRATEGY   auto|custom|k8s|compose (default: auto)
#   --namespace NAME      Kubernetes namespace (default: environment name)
#   --dry-run             Print actions/validate config without applying
#   --confirm-production  Required for production deploys
#   -h, --help            Show help
#
# Environment overrides:
#   DEPLOY_CMD
#   DEPLOY_DEV_CMD
#   DEPLOY_STAGING_CMD
#   DEPLOY_PROD_CMD
#
# Exit codes:
#   0  Deployment succeeded
#   1  Deployment command failed
#   2  Setup/usage/tooling error

ENVIRONMENT="$1"
if [ -n "$ENVIRONMENT" ]; then
  shift
fi

if [ "$ENVIRONMENT" = "-h" ] || [ "$ENVIRONMENT" = "--help" ]; then
  ENVIRONMENT=""
  set -- --help "$@"
fi

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
STRATEGY="${DEPLOY_STRATEGY:-auto}"
NAMESPACE="${DEPLOY_NAMESPACE:-}"
DRY_RUN=0
CONFIRM_PROD=0

print_usage() {
  cat <<EOF
Usage: $0 <environment> [options]

Arguments:
  environment            dev|staging|prod

Options:
  --strategy STRATEGY    auto|custom|k8s|compose (default: auto)
  --namespace NAME       Kubernetes namespace (default: environment name)
  --dry-run              Print actions/validate config without applying
  --confirm-production   Required for production deploys
  -h, --help             Show help
EOF
}

resolve_custom_cmd() {
  case "$ENVIRONMENT" in
    dev)
      if [ -n "${DEPLOY_DEV_CMD:-}" ]; then
        echo "$DEPLOY_DEV_CMD"
      else
        echo "${DEPLOY_CMD:-}"
      fi
      ;;
    staging|stage)
      if [ -n "${DEPLOY_STAGING_CMD:-}" ]; then
        echo "$DEPLOY_STAGING_CMD"
      else
        echo "${DEPLOY_CMD:-}"
      fi
      ;;
    prod|production)
      if [ -n "${DEPLOY_PROD_CMD:-}" ]; then
        echo "$DEPLOY_PROD_CMD"
      else
        echo "${DEPLOY_CMD:-}"
      fi
      ;;
    *)
      echo "${DEPLOY_CMD:-}"
      ;;
  esac
}

compose_available() {
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    return 0
  fi
  if command -v docker-compose >/dev/null 2>&1; then
    return 0
  fi
  return 1
}

run_compose() {
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    docker compose "$@"
  else
    docker-compose "$@"
  fi
}

resolve_base_compose_file() {
  for candidate in \
    "$ROOT_DIR/docker-compose.yml" \
    "$ROOT_DIR/docker-compose.yaml" \
    "$ROOT_DIR/compose.yml" \
    "$ROOT_DIR/compose.yaml"; do
    if [ -f "$candidate" ]; then
      echo "$candidate"
      return 0
    fi
  done
  return 1
}

resolve_env_compose_file() {
  for candidate in \
    "$ROOT_DIR/docker-compose.${ENVIRONMENT}.yml" \
    "$ROOT_DIR/docker-compose.${ENVIRONMENT}.yaml" \
    "$ROOT_DIR/compose.${ENVIRONMENT}.yml" \
    "$ROOT_DIR/compose.${ENVIRONMENT}.yaml"; do
    if [ -f "$candidate" ]; then
      echo "$candidate"
      return 0
    fi
  done
  return 1
}

compose_config_present() {
  if resolve_base_compose_file >/dev/null 2>&1; then
    return 0
  fi
  if resolve_env_compose_file >/dev/null 2>&1; then
    return 0
  fi
  return 1
}

k8s_target_mode() {
  if [ -f "$ROOT_DIR/k8s/${ENVIRONMENT}/kustomization.yaml" ] || [ -f "$ROOT_DIR/k8s/${ENVIRONMENT}/kustomization.yml" ]; then
    echo "kustomize-dir"
    return 0
  fi
  if [ -d "$ROOT_DIR/k8s/${ENVIRONMENT}" ] && find "$ROOT_DIR/k8s/${ENVIRONMENT}" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) -print -quit | grep -q .; then
    echo "manifest-dir"
    return 0
  fi
  if [ -f "$ROOT_DIR/k8s/${ENVIRONMENT}.yaml" ]; then
    echo "manifest-file-yaml"
    return 0
  fi
  if [ -f "$ROOT_DIR/k8s/${ENVIRONMENT}.yml" ]; then
    echo "manifest-file-yml"
    return 0
  fi
  return 1
}

run_custom() {
  custom_cmd="$1"
  if [ -z "$custom_cmd" ]; then
    echo "ERROR: no custom deploy command configured." >&2
    exit 2
  fi

  echo "Running custom deploy command for environment '${ENVIRONMENT}'"
  (
    cd "$ROOT_DIR" || exit 2
    DEPLOY_ENV="$ENVIRONMENT" DEPLOY_NAMESPACE="$NAMESPACE" sh -c "$custom_cmd"
  )
  return $?
}

run_compose_deploy() {
  if ! compose_available; then
    echo "ERROR: docker compose is not available." >&2
    exit 2
  fi

  base_file=$(resolve_base_compose_file || true)
  env_file=$(resolve_env_compose_file || true)

  if [ -z "$base_file" ] && [ -z "$env_file" ]; then
    echo "ERROR: no compose files found for deployment." >&2
    exit 2
  fi

  echo "Deploying with Docker Compose for '${ENVIRONMENT}'"
  if [ -n "$base_file" ]; then
    echo "  base file: $base_file"
  fi
  if [ -n "$env_file" ]; then
    echo "  env file:  $env_file"
  fi

  if [ "$DRY_RUN" -eq 1 ]; then
    echo "Dry run: validating compose configuration."
    if [ -n "$base_file" ] && [ -n "$env_file" ]; then
      run_compose -f "$base_file" -f "$env_file" config >/dev/null
    elif [ -n "$base_file" ]; then
      run_compose -f "$base_file" config >/dev/null
    else
      run_compose -f "$env_file" config >/dev/null
    fi
    return $?
  fi

  if [ -n "$base_file" ] && [ -n "$env_file" ]; then
    run_compose -f "$base_file" -f "$env_file" up -d --remove-orphans
  elif [ -n "$base_file" ]; then
    run_compose -f "$base_file" up -d --remove-orphans
  else
    run_compose -f "$env_file" up -d --remove-orphans
  fi
}

kubectl_apply() {
  if [ -n "$NAMESPACE" ]; then
    kubectl -n "$NAMESPACE" "$@"
  else
    kubectl "$@"
  fi
}

run_k8s_deploy() {
  if ! command -v kubectl >/dev/null 2>&1; then
    echo "ERROR: kubectl is not available." >&2
    exit 2
  fi

  mode=$(k8s_target_mode || true)
  if [ -z "$mode" ]; then
    echo "ERROR: no Kubernetes manifests found for environment '${ENVIRONMENT}'." >&2
    exit 2
  fi

  if [ -z "$NAMESPACE" ]; then
    NAMESPACE="$ENVIRONMENT"
  fi

  echo "Deploying with Kubernetes for '${ENVIRONMENT}' (namespace: ${NAMESPACE})"

  case "$mode" in
    kustomize-dir)
      target="$ROOT_DIR/k8s/${ENVIRONMENT}"
      if [ "$DRY_RUN" -eq 1 ]; then
        kubectl_apply apply -k "$target" --dry-run=client >/dev/null
      else
        kubectl_apply apply -k "$target"
      fi
      ;;
    manifest-dir)
      target="$ROOT_DIR/k8s/${ENVIRONMENT}"
      if [ "$DRY_RUN" -eq 1 ]; then
        kubectl_apply apply -f "$target" --dry-run=client >/dev/null
      else
        kubectl_apply apply -f "$target"
      fi
      ;;
    manifest-file-yaml)
      target="$ROOT_DIR/k8s/${ENVIRONMENT}.yaml"
      if [ "$DRY_RUN" -eq 1 ]; then
        kubectl_apply apply -f "$target" --dry-run=client >/dev/null
      else
        kubectl_apply apply -f "$target"
      fi
      ;;
    manifest-file-yml)
      target="$ROOT_DIR/k8s/${ENVIRONMENT}.yml"
      if [ "$DRY_RUN" -eq 1 ]; then
        kubectl_apply apply -f "$target" --dry-run=client >/dev/null
      else
        kubectl_apply apply -f "$target"
      fi
      ;;
    *)
      echo "ERROR: unsupported Kubernetes target mode: $mode" >&2
      exit 2
      ;;
  esac
}

while [ $# -gt 0 ]; do
  case "$1" in
    --strategy)
      STRATEGY="$2"
      shift 2
      ;;
    --namespace)
      NAMESPACE="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    --confirm-production)
      CONFIRM_PROD=1
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
  print_usage >&2
  exit 2
fi

case "$ENVIRONMENT" in
  dev|staging|stage|prod|production) ;;
  *)
    echo "ERROR: invalid environment '$ENVIRONMENT'." >&2
    exit 2
    ;;
esac

if [ "$ENVIRONMENT" = "prod" ] || [ "$ENVIRONMENT" = "production" ]; then
  if [ "$CONFIRM_PROD" -ne 1 ] && [ "${CONFIRM_PRODUCTION_DEPLOY:-}" != "yes" ]; then
    echo "ERROR: production deploy requires --confirm-production or CONFIRM_PRODUCTION_DEPLOY=yes." >&2
    exit 2
  fi
fi

custom_cmd=$(resolve_custom_cmd)

if [ "$STRATEGY" = "custom" ]; then
  run_custom "$custom_cmd"
  rc=$?
elif [ "$STRATEGY" = "compose" ]; then
  run_compose_deploy
  rc=$?
elif [ "$STRATEGY" = "k8s" ]; then
  run_k8s_deploy
  rc=$?
elif [ "$STRATEGY" = "auto" ]; then
  if [ -n "$custom_cmd" ]; then
    run_custom "$custom_cmd"
    rc=$?
  else
    if k8s_target_mode >/dev/null 2>&1 && command -v kubectl >/dev/null 2>&1; then
      run_k8s_deploy
      rc=$?
    elif compose_config_present && compose_available; then
      run_compose_deploy
      rc=$?
    else
      echo "ERROR: could not determine deploy strategy." >&2
      echo "Set DEPLOY_*_CMD, or add k8s manifests, or add docker compose files." >&2
      exit 2
    fi
  fi
else
  echo "ERROR: invalid strategy '$STRATEGY'." >&2
  exit 2
fi

if [ $rc -ne 0 ]; then
  echo "Deployment failed for '${ENVIRONMENT}' (exit ${rc})." >&2
  exit 1
fi

if [ "$DRY_RUN" -eq 1 ]; then
  echo "Dry run completed for '${ENVIRONMENT}'."
else
  echo "Deployment completed for '${ENVIRONMENT}'."
fi

exit 0
