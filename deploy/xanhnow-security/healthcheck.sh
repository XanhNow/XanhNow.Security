#!/usr/bin/env bash
set -Eeuo pipefail

base_url="${SECURITY_BASE_URL:-http://127.0.0.1:5200}"

curl -fsS "$base_url/health/live" >/dev/null
curl -fsS "$base_url/health/ready" >/dev/null

echo "XanhNow Security healthcheck passed: $base_url"
