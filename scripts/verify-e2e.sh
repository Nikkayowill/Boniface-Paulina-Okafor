#!/usr/bin/env sh
set -eu

DOTNET_BIN="${DOTNET_BIN:-dotnet}"
E2E_PROJECT="${E2E_PROJECT:-tests/Okafor.NET.E2E/Okafor.NET.E2E.csproj}"
OUTPUT_DIR="tests/Okafor.NET.E2E/bin/Debug/net10.0"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required for SQL Server E2E tests."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker is installed but the daemon is unavailable to this user."
  exit 1
fi

"$DOTNET_BIN" restore "$E2E_PROJECT"
"$DOTNET_BIN" build "$E2E_PROJECT" --no-restore --verbosity minimal

if [ "${E2E_INSTALL_BROWSERS:-0}" = "1" ]; then
  "$OUTPUT_DIR/.playwright/node/linux-x64/node" \
    "$OUTPUT_DIR/.playwright/package/cli.js" install chromium
fi

"$DOTNET_BIN" test "$E2E_PROJECT" --no-build --filter "Category=E2E" --verbosity minimal
