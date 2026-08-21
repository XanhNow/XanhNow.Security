#!/usr/bin/env bash
set -Eeuo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

install -d -o xanhnow -g xanhnow -m 0750 /etc/xanhnow/s101/security/vault
install -d -o xanhnow -g xanhnow -m 0750 /etc/xanhnow/s101/security/trust
install -d -o xanhnow -g xanhnow -m 0750 /etc/xanhnow/s101/security/templates
install -d -o xanhnow -g xanhnow -m 0700 /srv/xanhnow/s101/secrets/security

install -o root -g root -m 0644 \
  "$repo_root/deploy/xanhnow-security/vault-agent/vault-agent.hcl" \
  /etc/xanhnow/s101/security/vault-agent.hcl

install -o root -g root -m 0644 \
  "$repo_root/deploy/xanhnow-security/systemd/xanhnow-security-vault-agent.service" \
  /etc/systemd/system/xanhnow-security-vault-agent.service

install -o root -g root -m 0644 \
  "$repo_root/deploy/xanhnow-security/systemd/xanhnow-security-api.service" \
  /etc/systemd/system/xanhnow-security-api.service

install -o root -g root -m 0644 \
  "$repo_root/deploy/xanhnow-security/systemd/xanhnow-security-worker.service" \
  /etc/systemd/system/xanhnow-security-worker.service

install -o root -g root -m 0644 \
  "$repo_root/deploy/xanhnow-security/systemd/xanhnow-security-migrator.service" \
  /etc/systemd/system/xanhnow-security-migrator.service

install -o xanhnow -g xanhnow -m 0640 \
  "$repo_root"/deploy/xanhnow-security/vault-agent/templates/*.ctmpl \
  /etc/xanhnow/s101/security/templates/

chown -R xanhnow:xanhnow /srv/xanhnow/s101/secrets/security
chmod 0700 /srv/xanhnow/s101/secrets/security
find /srv/xanhnow/s101/secrets/security -type f -exec chmod 0600 {} +

systemctl daemon-reload
systemctl enable --now xanhnow-security-vault-agent.service
sleep 3
systemctl is-active --quiet xanhnow-security-vault-agent.service

test -s /srv/xanhnow/s101/secrets/security/postgres-connection-string
test -s /srv/xanhnow/s101/secrets/security/redis-configuration
test -s /srv/xanhnow/s101/secrets/security/redis-password
test -s /srv/xanhnow/s101/secrets/security/redis-key-prefix
test -s /srv/xanhnow/s101/secrets/security/kafka-bootstrap-servers
test -s /srv/xanhnow/s101/secrets/security/kafka-security-protocol
test -s /srv/xanhnow/s101/secrets/security/kafka-sasl-mechanism
test -s /srv/xanhnow/s101/secrets/security/kafka-username
test -s /srv/xanhnow/s101/secrets/security/kafka-password
test -s /srv/xanhnow/s101/secrets/security/grant-signing-key
test -s /srv/xanhnow/s101/secrets/security/.vault-token

echo "XanhNow Security Vault Agent installed and rendered runtime secrets."
