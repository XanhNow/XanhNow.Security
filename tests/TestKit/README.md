# XanhNow.Security TestKit

Shared test helpers for RB11 and later runbooks. This folder is linked into all test projects by `tests/Directory.Build.targets`; it is not a separate production or test project.

Rules:
- No calls to real Vault, PostgreSQL, Redis, Kafka or child apps from TestKit.
- No secrets, passwords, private keys, refresh tokens or TOTP secrets.
- Helpers stay deterministic so test runs are repeatable.
