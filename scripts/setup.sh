#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> Starting containers..."
docker compose -f "$ROOT/docker-compose.yml" -f "$ROOT/docker-compose.override.yml" up -d

echo "==> Waiting for PostgreSQL to be ready..."
until docker compose -f "$ROOT/docker-compose.yml" exec -T postgres \
  pg_isready -U lexify -d lexify > /dev/null 2>&1; do
  sleep 2
done

echo "==> Pulling Ollama model (qwen3:8b)..."
docker compose -f "$ROOT/docker-compose.yml" exec -T ollama \
  ollama pull qwen3:8b

echo "==> Applying EF Core migrations..."
dotnet ef database update \
  --project "$ROOT/backend/src/Lexify.Infrastructure" \
  --startup-project "$ROOT/backend/src/Lexify.API"

echo "==> Setup complete. Services running:"
echo "    PostgreSQL : localhost:5432"
echo "    Redis      : localhost:6379"
echo "    Ollama     : localhost:11434"
