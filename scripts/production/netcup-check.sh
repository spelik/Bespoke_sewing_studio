#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${PROJECT_ROOT:-/opt/apps/projects/bespoke-studio}"
HEALTH_URL_FILE="${HEALTH_URL_FILE:-/opt/apps/caddy/oksanalogosha-health-url.txt}"

cd "$PROJECT_ROOT"

echo "== Docker containers =="
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo "== Compose status =="
docker compose --env-file .env -f docker-compose.yml ps -a

echo "== App logs since 5m =="
docker compose --env-file .env -f docker-compose.yml logs --since=5m bespoke-studio-app

echo "== Local app version through forwarded headers =="
curl -fsS -i \
  -H "Host: oksanalogosha.com" \
  -H "X-Forwarded-Proto: https" \
  -H "X-Forwarded-Host: oksanalogosha.com" \
  http://127.0.0.1:5030/api/version

echo "== Public checks =="
curl -fsS -I https://oksanalogosha.com/
curl -fsS -I https://www.oksanalogosha.com/ || true

health_status="$(curl -sS -o /dev/null -w '%{http_code}' https://oksanalogosha.com/health)"
if [ "$health_status" != "404" ]; then
  echo "Expected public /health to be 404, got $health_status" >&2
  exit 1
fi
echo "Public /health is closed with 404."

if [ -f "$HEALTH_URL_FILE" ]; then
  echo "== Secret health check =="
  curl -fsS -i "$(cat "$HEALTH_URL_FILE")"
else
  echo "Secret health URL file is missing: $HEALTH_URL_FILE" >&2
  exit 1
fi

echo "Manual checks still required: admin login, dashboard, order form, contact form, upload scan, owner/customer email, Email Log/outbox, SignalR live updates."
