#!/usr/bin/env sh
set -eu

export DOTNET_USE_POLLING_FILE_WATCHER="${DOTNET_USE_POLLING_FILE_WATCHER:-1}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5187}"

exec "${DOTNET_ROOT:-$HOME/.dotnet}/dotnet" watch run \
  --project Okafor-.NET.csproj \
  --no-launch-profile
