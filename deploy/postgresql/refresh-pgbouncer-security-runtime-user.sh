#!/usr/bin/env bash
set -Eeuo pipefail

db_host="${DB_HOST:-192.168.2.80}"
db_port="${DB_PORT:-15432}"
db_name="${DB_NAME:-authtest}"
userlist="${PGBOUNCER_USERLIST:-/etc/pgbouncer/userlist.txt}"
runtime_user="s101_xanhnow_auth_security_runtime"

tmp="$(mktemp)"
fragment="$(mktemp)"
trap 'rm -f "$tmp" "$fragment"' EXIT

grep -vE '^"(s101_xanhnow_auth_security_runtime|xanhnow_security|xanhnow_security_api|xanhnow_security_worker)" ' "$userlist" > "$tmp" || true

sudo -u postgres psql -h "$db_host" -p "$db_port" -d "$db_name" -Atc \
  "select '\"' || rolname || '\" \"' || rolpassword || '\"' from pg_authid where rolname = '$runtime_user';" \
  > "$fragment"

cat "$tmp" "$fragment" > "$userlist"

if command -v systemctl >/dev/null 2>&1; then
  systemctl reload pgbouncer
else
  service pgbouncer reload
fi

awk '
{
  user=$1
  pass=$2
  gsub(/"/, "", user)
  if (user == "s101_xanhnow_auth_security_runtime") {
    if (pass ~ /^"SCRAM-SHA-256/) print user, "SCRAM"
    else if (pass ~ /^"md5/) print user, "MD5"
    else print user, "PLAIN_OR_OTHER"
  }
}
' "$userlist"
