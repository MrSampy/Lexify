#!/usr/bin/env bash
#
# Restore a Lexify Postgres dump produced by the `postgres-backup` service.
#
# Default (SAFE): restores into a throwaway database so you can verify the dump is actually good.
# This is the mode you should run *before* real users exist — an untested backup is not a backup.
#
#   ./restore-db.sh                             # verify the newest dump into a temp DB
#   ./restore-db.sh /backups/daily/xyz.sql.gz   # verify a specific dump
#   ./restore-db.sh --into-production <dump>    # DESTRUCTIVE: overwrite the live database
#
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
COMPOSE="docker compose -f $COMPOSE_FILE"
VERIFY_DB="lexify_restore_check"

TARGET="verify"
DUMP=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --into-production) TARGET="production"; shift ;;
    -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
    *) DUMP="$1"; shift ;;
  esac
done

# Newest dump wins when none was named.
if [[ -z "$DUMP" ]]; then
  DUMP=$($COMPOSE exec -T postgres-backup sh -c 'ls -1t /backups/daily/*.sql.gz 2>/dev/null | head -1' || true)
  [[ -n "$DUMP" ]] || { echo "No dump found in /backups/daily. Has the backup run yet?" >&2; exit 1; }
  echo "==> Using newest dump: $DUMP"
fi

psql_root() { $COMPOSE exec -T postgres psql -U lexify -d postgres -v ON_ERROR_STOP=1 "$@"; }

# The dump lives in the backup container's volume; stream it through to postgres.
stream_dump() { $COMPOSE exec -T postgres-backup sh -c "gunzip -c '$DUMP'"; }

if [[ "$TARGET" == "production" ]]; then
  echo "!! DESTRUCTIVE: this REPLACES the live 'lexify' database with $DUMP"
  read -rp "Type 'yes' to continue: " confirm
  [[ "$confirm" == "yes" ]] || { echo "Aborted."; exit 1; }

  echo "==> Stopping backend so nothing writes mid-restore..."
  $COMPOSE stop backend

  echo "==> Recreating database..."
  psql_root -c "DROP DATABASE IF EXISTS lexify WITH (FORCE);"
  psql_root -c "CREATE DATABASE lexify OWNER lexify;"

  echo "==> Restoring..."
  stream_dump | $COMPOSE exec -T postgres psql -U lexify -d lexify -v ON_ERROR_STOP=1 >/dev/null

  echo "==> Starting backend..."
  $COMPOSE start backend
  echo "==> Done. Live database restored from $DUMP"
  exit 0
fi

# ---- Verify mode: restore into a scratch DB and prove the data is really there ----
echo "==> Restoring into throwaway database '$VERIFY_DB' (live data untouched)..."
psql_root -c "DROP DATABASE IF EXISTS $VERIFY_DB WITH (FORCE);"
psql_root -c "CREATE DATABASE $VERIFY_DB OWNER lexify;"

stream_dump | $COMPOSE exec -T postgres psql -U lexify -d "$VERIFY_DB" -v ON_ERROR_STOP=1 >/dev/null

echo "==> Row counts in the restored copy:"
$COMPOSE exec -T postgres psql -U lexify -d "$VERIFY_DB" -c "
  SELECT 'users' AS table, COUNT(*) FROM users
  UNION ALL SELECT 'word_blocks', COUNT(*) FROM word_blocks
  UNION ALL SELECT 'words',       COUNT(*) FROM words
  UNION ALL SELECT 'tests',       COUNT(*) FROM tests;"

echo
echo "==> If those numbers look right, the dump is good."
echo "    Dropping the scratch database..."
psql_root -c "DROP DATABASE IF EXISTS $VERIFY_DB WITH (FORCE);"
echo "==> Verification complete."
