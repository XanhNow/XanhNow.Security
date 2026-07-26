using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Campaign;

namespace XanhNow.Security.ArchitectureTests;

public sealed class Rb16ProductionHardeningCampaignTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb16_campaign_defines_ordered_production_hardening_gates()
    {
        Assert.Equal("RB16 Production Hardening", Rb16ProductionHardeningCampaign.CampaignName);
        Assert.Equal("feature/rb16-production-hardening", Rb16ProductionHardeningCampaign.ReleaseCandidateBranch);

        Assert.Equal(20, Rb16ProductionHardeningCampaign.Gates.Count);
        Assert.Equal("release-candidate-lock", Rb16ProductionHardeningCampaign.Gates[0].Name);
        Assert.Equal("production-readiness-review", Rb16ProductionHardeningCampaign.Gates[^1].Name);

        Assert.Contains(Rb16ProductionHardeningCampaign.Gates, gate => gate.Name == "vault-secret-runtime-identity" && gate.RequiresRealInfrastructure);
        Assert.Contains(Rb16ProductionHardeningCampaign.Gates, gate => gate.Name == "api-edge-request-limits" && !gate.RequiresRealInfrastructure);
        Assert.Contains(Rb16ProductionHardeningCampaign.Gates, gate => gate.Name == "logging-audit-redaction" && !gate.RequiresRealInfrastructure);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb16_campaign_defines_required_evidence_without_duplicates()
    {
        Assert.Equal(20, Rb16ProductionHardeningCampaign.RequiredEvidenceFolders.Count);
        Assert.Equal(
            Rb16ProductionHardeningCampaign.RequiredEvidenceFolders.Count,
            Rb16ProductionHardeningCampaign.RequiredEvidenceFolders.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal("01-release-candidate", Rb16ProductionHardeningCampaign.RequiredEvidenceFolders[0]);
        Assert.Equal("20-readiness-review", Rb16ProductionHardeningCampaign.RequiredEvidenceFolders[^1]);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb16_campaign_inventory_does_not_contain_secret_material()
    {
        var rendered = string.Join(
            Environment.NewLine,
            Rb16ProductionHardeningCampaign.Gates.Select(gate => $"{gate.Name}|{gate.Purpose}|{gate.RequiresRealInfrastructure}")
                .Concat(Rb16ProductionHardeningCampaign.RequiredEvidenceFolders));

        SecretAssertions.DoesNotContainSecretMaterial(rendered);
    }
}
