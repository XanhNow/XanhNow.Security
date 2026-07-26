namespace XanhNow.Security.Tests.TestKit.Campaign;

public sealed record Rb17DeploymentGate(string Name, string Purpose, bool RequiresProductionChangeWindow);

public static class Rb17CiCdDeployRollbackCampaign
{
    public const string CampaignName = "RB17 CI CD Deploy Rollback";
    public const string ReleaseCandidateBranch = "feature/rb17-ci-cd-deploy-rollback";

    public static IReadOnlyList<Rb17DeploymentGate> Gates { get; } =
    [
        new("release-campaign-record", "Open the RB17 release campaign and record source commit, scope and approval owner.", false),
        new("source-ci-quality-gate", "Run source CI for format, build, tests and boundary scans before packaging.", false),
        new("immutable-artifacts", "Publish API, Worker and Migrator into immutable artifact folders.", false),
        new("release-manifest-checksum", "Create release.json and SHA256SUMS for every release artifact.", false),
        new("release-tag-source", "Publish the approved source tag and verify it matches the built commit.", false),
        new("xanhnowdeploy-runner-access", "Verify XanhnowDeploy, API_Deploy runner and SSH access to api-1/api-2/api-3.", true),
        new("runtime-directory-systemd", "Prepare runtime directories, service units and ownership for XanhNow.Security only.", true),
        new("production-preflight", "Check Vault, PostgreSQL, Redis, Kafka and node health before the change window.", true),
        new("migrator-plan", "Validate the release bundle and run Migrator plan without applying schema changes.", true),
        new("migrator-apply-validate", "Apply SecurityDbContext migration once and validate the security schema.", true),
        new("canary-rollback-drill", "Prove rollback on a drained canary node before deploying live traffic.", true),
        new("canary-api-deploy", "Deploy API to one canary node and verify live, ready, dependency and release metadata.", true),
        new("rolling-api-deploy", "Deploy API to remaining nodes one by one with health checks between nodes.", true),
        new("rolling-worker-deploy", "Deploy Worker with singleton/concurrency guard and post-start verification.", true),
        new("feature-gate-config", "Enable only approved feature gates and keep unsafe flows disabled by default.", true),
        new("systemd-release-health", "Verify systemd state, current symlink, release.json and service health on every node.", true),
        new("production-smoke", "Run safe production smoke tests through the public REST API surface.", true),
        new("observe-and-decide", "Observe logs, audit, metrics and outbox before deciding pass, rollback or forward-fix.", true),
        new("release-registry", "Record final deployed commit, release id, node list and evidence links.", false),
        new("rollback-forward-fix", "Keep rollback and forward-fix procedures ready without running database down migration.", true),
        new("post-release-handoff", "Hand off ownership, monitoring notes and next-run evidence after the release.", false)
    ];

    public static IReadOnlyList<string> RequiredEvidenceFolders { get; } =
    [
        "01-release-campaign",
        "02-source-ci",
        "03-immutable-artifacts",
        "04-release-manifest-checksum",
        "05-release-tag-source",
        "06-xanhnowdeploy-runner-access",
        "07-runtime-directory-systemd",
        "08-production-preflight",
        "09-migrator-plan",
        "10-migrator-apply-validate",
        "11-canary-rollback-drill",
        "12-canary-api-deploy",
        "13-rolling-api-deploy",
        "14-rolling-worker-deploy",
        "15-feature-gate-config",
        "16-systemd-release-health",
        "17-production-smoke",
        "18-observe-and-decide",
        "19-release-registry",
        "20-rollback-forward-fix",
        "21-post-release-handoff"
    ];
}
