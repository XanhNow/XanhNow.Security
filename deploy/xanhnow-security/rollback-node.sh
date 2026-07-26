#!/usr/bin/env bash
set -Eeuo pipefail

previous_release="${1:?previous release directory is required}"
app_root="${2:?app root is required}"
service_name="${3:?systemd service name is required}"

if [ ! -d "$previous_release" ] || [ ! -f "$previous_release/release.json" ]; then
  echo "FAIL: previous release is invalid: $previous_release" >&2
  exit 1
fi

ln -sfn "$previous_release" "$app_root/current"
systemctl daemon-reload
systemctl restart "$service_name"
systemctl is-active --quiet "$service_name"
