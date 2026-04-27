#!/bin/sh
set -eu

API_PORT="${API_PORT:-8080}"
WEB_PORT="${PORT:-3000}"

export ISS_API_BASE_URL="${ISS_API_BASE_URL:-http://127.0.0.1:${API_PORT}}"

mkdir -p /app/backend/App_Data

dotnet /app/backend/ISS.Api.dll --urls "http://0.0.0.0:${API_PORT}" &
api_pid=$!

cleanup() {
  kill "$api_pid" 2>/dev/null || true
}

trap cleanup INT TERM EXIT

cd /app/frontend
npm run start -- -p "${WEB_PORT}"
