#!/usr/bin/env python3
"""
Nebula API contract checks.

Validates story-critical constraints that are intentionally solution-specific:
- F0002-S0001 create-broker conflict behavior (POST /brokers -> 409)
- F0002-S0002 broker search query semantics and constraints
- Nebula baseline error contract profile (RFC 7807 ProblemDetails)

Usage:
    python3 planning-mds/testing/validate-nebula-api-contract.py
    python3 planning-mds/testing/validate-nebula-api-contract.py planning-mds/api/nebula-api.yaml
"""

import sys
from pathlib import Path
from typing import Any, Dict, List

import yaml

DEFAULT_SPEC_PATH = Path("planning-mds/api/nebula-api.yaml")
PROBLEM_DETAILS_REF = "#/components/schemas/ProblemDetails"


def load_spec(file_path: Path) -> Dict[str, Any]:
    with file_path.open("r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def resolve_local_ref(spec: Dict[str, Any], ref: str) -> Any:
    if not isinstance(ref, str) or not ref.startswith("#/"):
        return None

    node: Any = spec
    for token in ref[2:].split("/"):
        if not isinstance(node, dict) or token not in node:
            return None
        node = node[token]
    return node


def resolve_parameter(spec: Dict[str, Any], parameter: Dict[str, Any], errors: List[str]) -> Dict[str, Any]:
    if not isinstance(parameter, dict):
        return {}
    if "$ref" not in parameter:
        return parameter

    resolved = resolve_local_ref(spec, parameter["$ref"])
    if not isinstance(resolved, dict):
        errors.append(f"Invalid parameter reference: {parameter['$ref']}")
        return {}
    return resolved


def extract_response_schema_ref(
    spec: Dict[str, Any], response: Dict[str, Any], context: str, errors: List[str]
) -> str:
    if not isinstance(response, dict):
        errors.append(f"{context}: response must be an object")
        return ""

    response_object = response
    if "$ref" in response:
        response_object = resolve_local_ref(spec, response["$ref"])
        if not isinstance(response_object, dict):
            errors.append(f"{context}: invalid response reference {response['$ref']}")
            return ""

    content = response_object.get("content", {})
    json_content = content.get("application/problem+json")
    if not isinstance(json_content, dict):
        errors.append(f"{context}: response must define application/problem+json content")
        return ""

    schema = json_content.get("schema")
    if not isinstance(schema, dict):
        errors.append(f"{context}: response must define schema")
        return ""

    schema_ref = schema.get("$ref")
    if not schema_ref:
        errors.append(f"{context}: response schema must reference {PROBLEM_DETAILS_REF}")
        return ""

    return schema_ref


def validate_nebula_contract(spec: Dict[str, Any]) -> List[str]:
    errors: List[str] = []

    components = spec.get("components", {})
    schemas = components.get("schemas", {})
    responses = components.get("responses", {})

    if "ErrorResponse" in schemas:
        errors.append("Legacy ErrorResponse schema found; canonical schema must be ProblemDetails only")

    problem_details = schemas.get("ProblemDetails")
    if not isinstance(problem_details, dict):
        errors.append("Missing components.schemas.ProblemDetails")
    else:
        properties = problem_details.get("properties", {})
        required = set(problem_details.get("required", []))

        for field in ["type", "title", "status", "code", "traceId"]:
            if field not in properties:
                errors.append(f"ProblemDetails missing property: {field}")

        for field in ["type", "title", "status"]:
            if field not in required:
                errors.append(f"ProblemDetails must require field: {field}")

    reusable_error_responses = ["BadRequest", "Unauthorized", "Forbidden", "NotFound", "Conflict"]
    for response_name in reusable_error_responses:
        response = responses.get(response_name)
        if response is None:
            errors.append(f"Missing reusable response component: {response_name}")
            continue

        schema_ref = extract_response_schema_ref(
            spec,
            response,
            f"components.responses.{response_name}",
            errors,
        )
        if schema_ref and schema_ref != PROBLEM_DETAILS_REF:
            errors.append(
                f"components.responses.{response_name} must reference {PROBLEM_DETAILS_REF}, found {schema_ref}"
            )

    paths = spec.get("paths", {})
    brokers_path = paths.get("/brokers")
    if not isinstance(brokers_path, dict):
        errors.append("Missing /brokers path (required by F0002-S0001 and F0002-S0002)")
        return errors

    create_operation = brokers_path.get("post")
    if not isinstance(create_operation, dict):
        errors.append("Missing POST /brokers operation (F0002-S0001)")
    else:
        responses = create_operation.get("responses", {})
        if "409" not in responses:
            errors.append("POST /brokers must define 409 Conflict for duplicate license behavior (F0002-S0001)")

    search_operation = brokers_path.get("get")
    if not isinstance(search_operation, dict):
        errors.append("Missing GET /brokers operation (F0002-S0002)")
        return errors

    raw_parameters = search_operation.get("parameters", [])
    if not isinstance(raw_parameters, list):
        errors.append("GET /brokers parameters must be an array")
        return errors

    parameters = [resolve_parameter(spec, p, errors) for p in raw_parameters]

    query_param = next(
        (p for p in parameters if p.get("name") == "q" and p.get("in") == "query"),
        None,
    )
    if query_param is None:
        errors.append("GET /brokers must define query parameter 'q' (F0002-S0002)")
    else:
        schema = query_param.get("schema", {})
        if schema.get("type") != "string":
            errors.append("GET /brokers query parameter 'q' must be a string")
        if schema.get("minLength") != 1:
            errors.append("GET /brokers query parameter 'q' must set minLength: 1")
        if schema.get("maxLength") != 100:
            errors.append("GET /brokers query parameter 'q' must set maxLength: 100")

    status_param = next(
        (p for p in parameters if p.get("name") == "status" and p.get("in") == "query"),
        None,
    )
    if status_param is None:
        errors.append("GET /brokers must define optional query parameter 'status' (F0002-S0002)")
    else:
        status_schema = status_param.get("schema", {})
        enum_values = status_schema.get("enum", [])
        if not isinstance(enum_values, list):
            errors.append("GET /brokers query parameter 'status' must define enum values")
        elif not {"Active", "Inactive"}.issubset(set(enum_values)):
            errors.append("GET /brokers query parameter 'status' must include Active and Inactive")

    return errors


def main() -> None:
    path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SPEC_PATH

    print(f"Validating Nebula API contract checks: {path}")
    print("-" * 60)

    try:
        spec = load_spec(path)
    except Exception as exc:
        print(f"[FAIL] Could not load API contract: {exc}")
        sys.exit(1)

    errors = validate_nebula_contract(spec)
    if errors:
        print(f"[FAIL] {len(errors)} error(s) found:")
        for idx, error in enumerate(errors, 1):
            print(f"  {idx}. {error}")
        sys.exit(1)

    print("[PASS] Nebula API contract checks passed.")
    sys.exit(0)


if __name__ == "__main__":
    main()
