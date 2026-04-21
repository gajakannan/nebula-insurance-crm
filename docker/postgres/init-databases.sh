#!/bin/bash
# Creates additional databases required by Nebula services.
# Mounted into /docker-entrypoint-initdb.d/ and runs once on first container start.
# The default "nebula" database is already created via POSTGRES_DB.

set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    SELECT 'CREATE DATABASE authentik'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'authentik')\gexec

    SELECT 'CREATE DATABASE temporal'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'temporal')\gexec

    SELECT 'CREATE DATABASE temporal_visibility'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'temporal_visibility')\gexec

    SELECT 'CREATE DATABASE pactbroker'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'pactbroker')\gexec

    SELECT 'CREATE DATABASE sonarqube'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'sonarqube')\gexec
EOSQL
