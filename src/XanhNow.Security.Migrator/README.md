# XanhNow.Security Migrator

RB10 builds the one-shot migration host for the `security` schema.

Supported modes:

```text
validate
plan
apply
```

The migrator uses a dedicated migration credential. Production configuration expects that credential from Vault. Development can use an approved environment variable for local validation. The process must not log complete connection strings or secret values.
