#!/usr/bin/env bash
set -euo pipefail

DOTNET_BIN="${DOTNET_BIN:-dotnet}"
if ! command -v "$DOTNET_BIN" >/dev/null 2>&1; then
  if [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_BIN="$HOME/.dotnet/dotnet"
  else
    echo "dotnet was not found on PATH. Install the .NET SDK or set DOTNET_BIN=/path/to/dotnet."
    exit 127
  fi
fi

RUN_SMOKE="${RUN_SMOKE:-1}"
RUN_DOCKER_STATUS="${RUN_DOCKER_STATUS:-1}"
RUN_NPM_BUILD="${RUN_NPM_BUILD:-0}"
LOG_DIR="${LOG_DIR:-docs/loop-runs}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
LOG_FILE="$LOG_DIR/$TIMESTAMP.md"

mkdir -p "$LOG_DIR"

run_and_log() {
  title="$1"
  shift
  {
    echo
    echo "## $title"
    echo
    echo '```text'
  } | tee -a "$LOG_FILE"

  set +e
  "$@" 2>&1 | tee -a "$LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e

  {
    echo '```'
    echo
    echo "Exit code: $status"
  } | tee -a "$LOG_FILE"

  return "$status"
}

log_git_status() {
  title="Git Status"
  {
    echo
    echo "## $title"
    echo
    echo '```text'
  } | tee -a "$LOG_FILE"

  set +e
  status_output="$(git status --short 2>&1)"
  status=$?
  set -e

  if [ "$status" -eq 0 ]; then
    total_lines="$(printf '%s\n' "$status_output" | sed '/^$/d' | wc -l | tr -d ' ')"
    if [ "$total_lines" -gt 200 ]; then
      printf '%s\n' "$status_output" | sed -n '1,200p' | tee -a "$LOG_FILE"
      {
        echo "... omitted $((total_lines - 200)) additional status lines ..."
        echo
        echo "Summary by status:"
      } | tee -a "$LOG_FILE"
      printf '%s\n' "$status_output" | awk 'NF { count[$1]++ } END { for (status in count) printf "%s %s\n", count[status], status }' | sort | tee -a "$LOG_FILE"
    elif [ "$total_lines" -gt 0 ]; then
      printf '%s\n' "$status_output" | tee -a "$LOG_FILE"
    else
      echo "Working tree clean." | tee -a "$LOG_FILE"
    fi
  else
    printf '%s\n' "$status_output" | tee -a "$LOG_FILE"
  fi

  {
    echo '```'
    echo
    echo "Exit code: $status"
  } | tee -a "$LOG_FILE"

  return "$status"
}

{
  echo "# Functionality Loop Run"
  echo
  echo "- Timestamp UTC: $TIMESTAMP"
  echo "- Working directory: $(pwd)"
  echo "- RUN_SMOKE: $RUN_SMOKE"
  echo "- RUN_NPM_BUILD: $RUN_NPM_BUILD"
  echo
  echo "## Loop Sources"
  echo
  echo "- docs/FUNCTIONALITY_LOOP.md"
  echo "- docs/FUNCTIONALITY_LOOP_BOARD.md"
  echo "- docs/RECOVERY_STATUS.md"
  echo "- docs/FEATURE_INVENTORY.md"
  echo "- docs/VERIFICATION_CHECKLIST.md"
} > "$LOG_FILE"

log_git_status

if [ "$RUN_DOCKER_STATUS" = "1" ] && command -v docker >/dev/null 2>&1; then
  run_and_log "Docker Compose Status" docker compose ps || true
fi

if [ "$RUN_NPM_BUILD" = "1" ]; then
  run_and_log "Tailwind Build" npm run build:css
fi

if [ "$RUN_SMOKE" = "1" ]; then
  export RUN_SMOKE=1
  export DOTNET_BIN
  run_and_log "Backend Verification With Smoke" ./scripts/verify-backend.sh
else
  export DOTNET_BIN
  run_and_log "Backend Verification" ./scripts/verify-backend.sh
fi

{
  echo
  echo "## Next Human/Codex Step"
  echo
  echo "Read docs/FUNCTIONALITY_LOOP_BOARD.md and select one unchecked Codex-lane task."
  echo "Leave Owner-lane tasks unchecked unless the owner has explicitly confirmed completion."
} | tee -a "$LOG_FILE"

echo
echo "Loop evidence written to $LOG_FILE"
