Scripts for XanhNow.Security local and CI quality gates.

- `test-fast.ps1`: runs the fast test gate without real shared infrastructure.
- `test-integration.ps1`: runs integration and end-to-end tests without external fixtures by default.
- `test-all.ps1`: runs the full solution test gate.
- `test-rb15.ps1`: runs the RB15 integration/system test campaign gate. It does not use real Vault, PostgreSQL, Redis, Kafka or child apps unless `-EnableExternalFixtures` is passed.
- `test-rb16.ps1`: runs the RB16 production hardening campaign gate for architecture, API, Worker, Migrator and integration foundations. It does not use real Vault, PostgreSQL, Redis, Kafka or child apps unless `-EnableExternalFixtures` is passed.
- `build-release.ps1`: publishes immutable API, Worker and Migrator release folders and creates release manifest/checksums.
- `validate-release-bundle.ps1`: validates RB17 release.json, SHA256SUMS and required publish folders.
- `test-rb17.ps1`: runs the RB17 CI/CD deploy rollback campaign gate. It does not use real Vault, PostgreSQL, Redis, Kafka or child apps unless `-EnableExternalFixtures` is passed.

