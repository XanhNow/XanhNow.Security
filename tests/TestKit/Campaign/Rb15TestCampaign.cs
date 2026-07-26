using XanhNow.Security.Tests.TestKit.ChildApps;

namespace XanhNow.Security.Tests.TestKit.Campaign;

public sealed record Rb15CampaignSuite(string Name, string Scope, bool RequiresExternalFixture);

public sealed record Rb15CampaignDependency(string Name, string Purpose, bool UsesRealRuntimeInSharedSystemEnvironment);

public static class Rb15TestCampaign
{
    public const string CampaignName = "RB15 Integration va System Test";
    public const string ReleaseCandidateBranch = "feature/rb15-integration-system-test";

    public static IReadOnlyList<Rb15CampaignSuite> Suites { get; } =
    [
        new("smoke", "API/Worker/Migrator can start and expose health without leaking secrets.", false),
        new("contract-compatibility", "XanhNow.Security client contracts match the four child app boundaries.", false),
        new("core-e2e", "Register, login, token, passkey, Smart OTP and step-up orchestration paths are covered.", false),
        new("account-security-e2e", "Password, phone, account state, session and passkey management paths are covered.", false),
        new("recovery-policy-stepup-e2e", "Policy, grant, lost device, account recovery and composite operations are covered.", false),
        new("shared-system", "The same suites can run against Vault, PostgreSQL, Redis, Kafka and child apps when explicitly enabled.", true)
    ];

    public static IReadOnlyList<Rb15CampaignDependency> Dependencies { get; } =
    [
        new("Vault", "Runtime secrets, grants and trust material.", true),
        new("PostgreSQL", "Security schema, audit, outbox, policy, recovery and operation state.", true),
        new("Redis", "Cache, idempotency, rate limit and distributed lock state.", true),
        new("Kafka", "Security event and audit publication.", true),
        new("Auth_Login_App", "Phone/password account identity and account state.", true),
        new("JWT_Refresh_Token_App", "Access token, refresh token reference and session state.", true),
        new("Passkey_Provider_App", "Passkey ceremony and credential state.", true),
        new("SmartOtp_App", "TOTP-based MFA and transaction step-up.", true)
    ];

    public static IReadOnlyList<string> RequiredEvidenceFolders { get; } =
    [
        "01-input",
        "02-capability",
        "03-compatibility",
        "04-environment",
        "05-migrator",
        "06-health",
        "07-test-data",
        "08-smoke",
        "09-contract",
        "10-core-e2e",
        "11-account-e2e",
        "12-policy-recovery-stepup",
        "13-database",
        "14-idempotency-replay",
        "15-failure-injection",
        "16-concurrency",
        "17-security-negative",
        "18-observability",
        "19-regression",
        "20-report"
    ];

    public static IReadOnlyDictionary<string, string> ChildAppCompatibilityMatrix { get; } =
        ChildAppContractCatalog.All.ToDictionary(
            target => target.AppName,
            target => $"{target.Protocol}|{target.RuntimePort}|{target.ContractOwner}",
            StringComparer.Ordinal);
}
