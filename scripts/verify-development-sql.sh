#!/usr/bin/env sh
set -eu

DOTNET_BIN="${DOTNET_BIN:-dotnet}"
PROJECT="${PROJECT:-Okafor-.NET.csproj}"
SERVICE="${POSTGRES_COMPOSE_SERVICE:-postgres}"
VERIFY_PORT="${OKAFOR_DEVELOPMENT_VERIFY_PORT:-5191}"
BASE_URL="http://127.0.0.1:${VERIFY_PORT}"
APP_LOG="${OKAFOR_DEVELOPMENT_VERIFY_LOG:-/tmp/okafor-development-sql.log}"

if ! command -v "$DOTNET_BIN" >/dev/null 2>&1; then
  if [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_BIN="$HOME/.dotnet/dotnet"
  else
    echo "dotnet was not found on PATH. Install the .NET SDK or set DOTNET_BIN."
    exit 127
  fi
fi

for command_name in docker curl; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "$command_name was not found on PATH."
    exit 127
  fi
done

if [ ! -f .env ]; then
  echo ".env is missing. Copy .env.example to .env and set the PostgreSQL password first."
  exit 1
fi

if ! grep -Eq '^POSTGRES_PASSWORD=.+$' .env || ! grep -Eq '^DATABASE_URL=.+$' .env; then
  echo ".env must define POSTGRES_PASSWORD and DATABASE_URL. Start from .env.example."
  exit 1
fi

cleanup() {
  if [ "${APP_PID:-}" ] && kill -0 "$APP_PID" 2>/dev/null; then
    kill "$APP_PID" 2>/dev/null || true
    wait "$APP_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "Starting PostgreSQL service '$SERVICE'..."
docker compose up -d "$SERVICE"

echo "Waiting for PostgreSQL to accept queries..."
attempt=0
until docker compose exec -T "$SERVICE" sh -c \
  'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
  >/dev/null 2>&1; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 60 ]; then
    echo "PostgreSQL did not become ready within 120 seconds."
    docker compose ps "$SERVICE" || true
    exit 1
  fi
  sleep 2
done

echo "Building the application..."
"$DOTNET_BIN" build "$PROJECT" --verbosity minimal

echo "Applying pending EF Core migrations..."
ASPNETCORE_ENVIRONMENT=Development \
  "$DOTNET_BIN" run --project "$PROJECT" --no-build --no-launch-profile -- --migrate-db

echo "Starting Development verification host at $BASE_URL..."
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="$BASE_URL" \
  "$DOTNET_BIN" run --project "$PROJECT" --no-build --no-launch-profile >"$APP_LOG" 2>&1 &
APP_PID=$!

attempt=0
until curl -fsS "$BASE_URL/health/live" >/dev/null 2>&1; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 60 ]; then
    echo "The application did not become live within 60 seconds. Recent log:"
    tail -n 80 "$APP_LOG" || true
    exit 1
  fi
  sleep 1
done

if ! curl -fsS "$BASE_URL/health/ready" >/dev/null; then
  echo "The application is live but PostgreSQL readiness failed. Recent log:"
  tail -n 80 "$APP_LOG" || true
  exit 1
fi

echo "Development SQL verification passed."
echo "- PostgreSQL accepted a query."
echo "- EF Core migrations completed."
echo "- /health/live returned success."
echo "- /health/ready returned success."
echo "PostgreSQL remains running for local development."
