using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Campaign;

namespace XanhNow.Security.ArchitectureTests;

public sealed class Rb15CampaignInventoryTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb15_campaign_defines_ordered_suites_dependencies_and_evidence()
    {
        Assert.Equal("RB15 Integration va System Test", Rb15TestCampaign.CampaignName);
        Assert.Equal("feature/rb15-integration-system-test", Rb15TestCampaign.ReleaseCandidateBranch);

        Assert.Equal(
            ["smoke", "contract-compatibility", "core-e2e", "account-security-e2e", "recovery-policy-stepup-e2e", "shared-system"],
            Rb15TestCampaign.Suites.Select(suite => suite.Name).ToArray());

        Assert.Equal("shared-system", Assert.Single(Rb15TestCampaign.Suites, suite => suite.RequiresExternalFixture).Name);

        Assert.Equal(20, Rb15TestCampaign.RequiredEvidenceFolders.Count);
        Assert.Equal(
            Rb15TestCampaign.RequiredEvidenceFolders.Count,
            Rb15TestCampaign.RequiredEvidenceFolders.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("01-input", Rb15TestCampaign.RequiredEvidenceFolders[0]);
        Assert.Equal("20-report", Rb15TestCampaign.RequiredEvidenceFolders[^1]);

        Assert.Equal(
            ["Vault", "PostgreSQL", "Redis", "Kafka", "Auth_Login_App", "JWT_Refresh_Token_App", "Passkey_Provider_App", "SmartOtp_App"],
            Rb15TestCampaign.Dependencies.Select(dependency => dependency.Name).ToArray());
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb15_campaign_inventory_does_not_contain_secret_material()
    {
        var rendered = string.Join(
            Environment.NewLine,
            Rb15TestCampaign.Suites.Select(suite => $"{suite.Name}|{suite.Scope}|{suite.RequiresExternalFixture}")
                .Concat(Rb15TestCampaign.Dependencies.Select(dependency => $"{dependency.Name}|{dependency.Purpose}|{dependency.UsesRealRuntimeInSharedSystemEnvironment}"))
                .Concat(Rb15TestCampaign.RequiredEvidenceFolders)
                .Concat(Rb15TestCampaign.ChildAppCompatibilityMatrix.Select(pair => $"{pair.Key}|{pair.Value}")));

        SecretAssertions.DoesNotContainSecretMaterial(rendered);
    }
}
