#!/usr/bin/env bash
set -euo pipefail

DB_NAME="${DB_NAME:-Apteka}"
PG_BIN="${PG_BIN:-/opt/homebrew/opt/postgresql@17/bin}"
PG_DATA="${PG_DATA:-/opt/homebrew/var/postgresql@17}"
PG_LOG="${PG_LOG:-/private/tmp/apteka_postgres.log}"

if ! "${PG_BIN}/pg_isready" -q; then
  "${PG_BIN}/pg_ctl" -D "${PG_DATA}" -l "${PG_LOG}" start
fi

if ! "${PG_BIN}/psql" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '${DB_NAME}'" | grep -q 1; then
  "${PG_BIN}/createdb" "${DB_NAME}"
fi

for migration in db/migrations/*.sql; do
  "${PG_BIN}/psql" -d "${DB_NAME}" -f "${migration}"
done
"${PG_BIN}/psql" -d "${DB_NAME}" -f db/seeds/001_demo_data.sql

echo "Database ${DB_NAME} is ready."
