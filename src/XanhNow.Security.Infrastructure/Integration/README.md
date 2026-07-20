# XanhNow.Security Infrastructure Integration

RB07 owns Infrastructure adapters for external systems and child apps.

Allowed in this folder:
- HTTP/gRPC transport adapters for Auth_Login_App, JWT_Refresh_Token_App, Passkey_Provider_App and SmartOtp_App.
- Vault, Redis and Kafka runtime integration boundaries.
- Deadline, redaction, downstream error mapping and integration registration.

Not allowed in this folder:
- REST controllers.
- Business orchestration use cases.
- Security database migration changes.
- Hard-coded passwords, AppRole SecretId values, Redis passwords, Kafka credentials, private keys or TOTP secrets.
