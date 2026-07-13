#!/usr/bin/env sh
set -eu

DOTNET_BIN="${DOTNET_BIN:-dotnet}"
if ! command -v "$DOTNET_BIN" >/dev/null 2>&1; then
  if [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_BIN="$HOME/.dotnet/dotnet"
  else
    echo "dotnet was not found on PATH. Install the .NET SDK or set DOTNET_BIN=/path/to/dotnet."
    exit 127
  fi
fi
PROJECT="${PROJECT:-Okafor-.NET.csproj}"
TEST_PROJECT="${TEST_PROJECT:-tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj}"
RUN_SMOKE="${RUN_SMOKE:-0}"
OKAFOR_VERIFY_PORT="${OKAFOR_VERIFY_PORT:-5187}"
OKAFOR_BASE_URL="${OKAFOR_BASE_URL:-http://localhost:${OKAFOR_VERIFY_PORT}}"
VERIFY_LOG="${VERIFY_LOG:-/tmp/okafor-verify-backend.log}"

echo "Restoring packages..."
"$DOTNET_BIN" restore "$PROJECT"

echo "Building test project..."
"$DOTNET_BIN" build "$TEST_PROJECT" --no-restore --verbosity minimal

echo "Running non-smoke, non-container tests..."
"$DOTNET_BIN" test "$TEST_PROJECT" --no-build --filter "Category!=Smoke&Category!=DatabaseIntegration" --verbosity minimal

if [ "$RUN_SMOKE" != "1" ]; then
  echo "Smoke tests skipped. Set RUN_SMOKE=1 to start the app and run smoke tests."
  exit 0
fi

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Testing}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-$OKAFOR_BASE_URL}"
export OKAFOR_BASE_URL

echo "Starting app for smoke tests at $ASPNETCORE_URLS..."
"$DOTNET_BIN" run --project "$PROJECT" --no-build --no-launch-profile > "$VERIFY_LOG" 2>&1 &
APP_PID=$!

cleanup() {
  if kill -0 "$APP_PID" 2>/dev/null; then
    kill "$APP_PID" 2>/dev/null || true
    wait "$APP_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

tries=0
until curl -fsS "$OKAFOR_BASE_URL/health" >/dev/null 2>&1; do
  tries=$((tries + 1))
  if [ "$tries" -ge 60 ]; then
    echo "App did not become healthy. Recent log:"
    tail -n 80 "$VERIFY_LOG" || true
    exit 1
  fi
  sleep 1
done

echo "Running smoke tests..."
"$DOTNET_BIN" test "$TEST_PROJECT" --no-build --filter "Category=Smoke" --verbosity minimal

echo "Backend verification complete."
