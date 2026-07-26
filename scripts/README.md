Scripts for XanhNow.Security local and CI quality gates.

- `test-fast.ps1`: runs the fast test gate without real shared infrastructure.
- `test-integration.ps1`: runs integration and end-to-end tests without external fixtures by default.
- `test-all.ps1`: runs the full solution test gate.
- `test-rb15.ps1`: runs the RB15 integration/system test campaign gate. It does not use real Vault, PostgreSQL, Redis, Kafka or child apps unless `-EnableExternalFixtures` is passed.
