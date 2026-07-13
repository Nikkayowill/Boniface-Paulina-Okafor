#!/usr/bin/env sh
set -eu

DOTNET_BIN="${DOTNET_BIN:-dotnet}"
TEST_PROJECT="${TEST_PROJECT:-tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required for SQL Server integration tests."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker is installed but the daemon is unavailable to this user."
  exit 1
fi

"$DOTNET_BIN" restore "$TEST_PROJECT"
"$DOTNET_BIN" build "$TEST_PROJECT" --no-restore --verbosity minimal
"$DOTNET_BIN" test "$TEST_PROJECT" --no-build --filter "Category=DatabaseIntegration" --verbosity minimal
