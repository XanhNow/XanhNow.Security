pid_file = "/srv/xanhnow/s101/secrets/security/vault-agent.pid"

vault {
  address = "https://192.168.2.81:8200"
  ca_cert = "/etc/xanhnow/s101/security/trust/vault-ca.crt"
}

auto_auth {
  method "approle" {
    mount_path = "auth/approle"
    config = {
      role_id_file_path = "/etc/xanhnow/s101/security/vault/role_id"
      secret_id_file_path = "/etc/xanhnow/s101/security/vault/secret_id"
      remove_secret_id_file_after_reading = false
    }
  }

  sink "file" {
    config = {
      path = "/srv/xanhnow/s101/secrets/security/.vault-token"
      mode = 0600
    }
  }
}

template {
  source = "/etc/xanhnow/s101/security/templates/postgres-connection-string.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/postgres-connection-string"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/redis-configuration.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/redis-configuration"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/redis-password.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/redis-password"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/redis-key-prefix.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/redis-key-prefix"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/kafka-bootstrap-servers.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/kafka-bootstrap-servers"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/kafka-username.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/kafka-username"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/kafka-password.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/kafka-password"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/kafka-security-protocol.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/kafka-security-protocol"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/kafka-sasl-mechanism.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/kafka-sasl-mechanism"
  perms = "0600"
}

template {
  source = "/etc/xanhnow/s101/security/templates/grant-signing-key.ctmpl"
  destination = "/srv/xanhnow/s101/secrets/security/grant-signing-key"
  perms = "0600"
}
