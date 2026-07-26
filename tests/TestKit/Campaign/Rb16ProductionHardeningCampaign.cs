namespace XanhNow.Security.Tests.TestKit.Campaign;

public sealed record Rb16HardeningGate(string Name, string Purpose, bool RequiresRealInfrastructure);

public static class Rb16ProductionHardeningCampaign
{
    public const string CampaignName = "RB16 Production Hardening";
    public const string ReleaseCandidateBranch = "feature/rb16-production-hardening";

    public static IReadOnlyList<Rb16HardeningGate> Gates { get; } =
    [
        new("release-candidate-lock", "Lock the release candidate source, version and evidence before hardening.", false),
        new("defect-risk-register", "Close known defects and record accepted risks before production readiness review.", false),
        new("configuration-startup-validation", "Validate runtime configuration during startup without leaking secret values.", false),
        new("vault-secret-runtime-identity", "Verify Vault AppRole, policy paths, grant key path and secret redaction.", true),
        new("mtls-certificate-rotation", "Verify server/client trust material, certificate expiry and rotation procedure.", true),
        new("postgresql-role-permission", "Verify security schema ownership, migration role and runtime role boundaries.", true),
        new("redis-kafka-runtime", "Verify cache, lock, idempotency, outbox publish and retry runtime dependencies.", true),
        new("api-edge-request-limits", "Verify headers, body limits, correlation id limits, HTTPS/HSTS and public API boundary.", false),
        new("authentication-authorization-feature-gate", "Verify caller identity, authorization policies and feature gates.", false),
        new("policy-grant-anti-replay", "Verify protected grants, replay prevention and policy decision audit trail.", false),
        new("timeout-retry-concurrency", "Tune downstream timeout, retry, concurrency and connection pool boundaries.", false),
        new("worker-outbox-retention", "Verify worker shutdown, retry, dead-letter and retention behavior.", false),
        new("load-stress-soak", "Execute workload, stress, spike and soak tests against the selected environment.", true),
        new("failover-restart-drill", "Exercise node restart and dependency failover without data corruption.", true),
        new("backup-restore-rpo-rto", "Verify backup, restore, RPO/RTO and data lifecycle drill.", true),
        new("logging-audit-redaction", "Verify log, audit, trace and error responses do not expose secret material.", false),
        new("metrics-alert-slo", "Verify metrics, dashboard, alert and SLO ownership.", true),
        new("security-scan-threat-review", "Run security scan, dependency review and threat review.", false),
        new("operational-readiness", "Confirm ownership, escalation, rollback and handoff material.", false),
        new("production-readiness-review", "Complete readiness review before moving to RB17 deploy and rollback.", false)
    ];

    public static IReadOnlyList<string> RequiredEvidenceFolders { get; } =
    [
        "01-release-candidate",
        "02-defects-risk",
        "03-config-startup",
        "04-vault",
        "05-mtls",
        "06-postgresql",
        "07-redis-kafka",
        "08-api-edge",
        "09-authz-feature-gate",
        "10-policy-grant",
        "11-timeout-retry",
        "12-worker-outbox",
        "13-load",
        "14-stress-soak",
        "15-failover",
        "16-backup-restore",
        "17-logging-redaction",
        "18-metrics-alert",
        "19-security-review",
        "20-readiness-review"
    ];
}
