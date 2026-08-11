#!/bin/sh
set -eu

API_PORT="${API_PORT:-8080}"
WEB_PORT="${PORT:-3000}"

export NEUEDGE_API_BASE_URL="${NEUEDGE_API_BASE_URL:-http://127.0.0.1:${API_PORT}}"

mkdir -p /app/backend/App_Data

dotnet /app/backend/ISS.Api.dll --urls "http://0.0.0.0:${API_PORT}" &
api_pid=$!

if [ "${SEED_SERVICE_DEMO_DATA:-false}" = "true" ]; then
  : "${Auth__BootstrapAdminEmail:?Auth__BootstrapAdminEmail is required for demo seeding}"
  : "${Auth__BootstrapAdminPassword:?Auth__BootstrapAdminPassword is required for demo seeding}"

  echo "Waiting for the API before seeding service demo data..."
  seed_attempt=0
  until curl -fsS "http://127.0.0.1:${API_PORT}/health" >/dev/null; do
    seed_attempt=$((seed_attempt + 1))
    if ! kill -0 "${api_pid}" 2>/dev/null; then
      echo "API exited before service demo data could be seeded." >&2
      wait "${api_pid}"
      exit $?
    fi
    if [ "${seed_attempt}" -ge 90 ]; then
      echo "Timed out waiting for the API before service demo data seeding." >&2
      exit 1
    fi
    sleep 2
  done

  python3 /app/scripts/seed-service-test-data.py \
    --base "http://127.0.0.1:${API_PORT}" \
    --email "${Auth__BootstrapAdminEmail}" \
    --password "${Auth__BootstrapAdminPassword}"
fi

cleanup() {
  kill "$api_pid" 2>/dev/null || true
}

trap cleanup INT TERM EXIT

cd /app/frontend
npm run start -- -p "${WEB_PORT}"
