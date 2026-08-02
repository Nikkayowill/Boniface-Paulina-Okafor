#!/usr/bin/env sh
set -eu

SERVICE="${POSTGRES_COMPOSE_SERVICE:-postgres}"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker was not found on PATH."
  exit 127
fi

if ! docker compose ps "$SERVICE" >/dev/null 2>&1; then
  echo "Docker Compose service '$SERVICE' is not available."
  exit 1
fi

admin_count="$(
  docker compose exec -T "$SERVICE" sh -c \
    'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tA -c "$1"' sh \
    'SELECT COUNT(*) FROM "AspNetUsers" u JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" JOIN "AspNetRoles" r ON ur."RoleId" = r."Id" WHERE r."Name" = '\''Admin'\'';'
)"

case "$admin_count" in
  ''|*[!0-9]*)
    echo "Could not determine seeded admin count."
    exit 1
    ;;
esac

if [ "$admin_count" -lt 1 ]; then
  echo "No user is currently assigned to the Admin role."
  echo "Set SeedAdmin:Email and SeedAdmin:Password with user secrets, then restart the app."
  exit 1
fi

echo "Admin role assignment exists."
