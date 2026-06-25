#!/usr/bin/env bash
# PostgreSQL backup → gzip → S3/Backblaze
# Usage: ./backup.sh
# Required env vars: POSTGRES_HOST, POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD,
#                    S3_BUCKET (e.g. s3://my-bucket/lexify-backups or b2:bucket/backups)
set -euo pipefail

TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="lexify_${TIMESTAMP}.sql.gz"
TMP_PATH="/tmp/${BACKUP_FILE}"

: "${POSTGRES_HOST:=postgres}"
: "${POSTGRES_DB:=lexify}"
: "${POSTGRES_USER:=lexify}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${S3_BUCKET:?S3_BUCKET is required}"

echo "==> Dumping database ${POSTGRES_DB}..."
PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump \
  -h "${POSTGRES_HOST}" \
  -U "${POSTGRES_USER}" \
  -d "${POSTGRES_DB}" \
  --no-owner \
  --no-acl \
  | gzip > "${TMP_PATH}"

echo "==> Uploading ${BACKUP_FILE} to ${S3_BUCKET}..."
if command -v rclone &>/dev/null; then
  rclone copy "${TMP_PATH}" "${S3_BUCKET}/"
elif command -v aws &>/dev/null; then
  aws s3 cp "${TMP_PATH}" "${S3_BUCKET}/${BACKUP_FILE}"
else
  echo "ERROR: Neither rclone nor aws CLI found. Install one to upload backups." >&2
  exit 1
fi

rm -f "${TMP_PATH}"
echo "==> Backup complete: ${BACKUP_FILE}"
