using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Campaign;

namespace XanhNow.Security.ArchitectureTests;

public sealed class Rb17CiCdDeployRollbackCampaignTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_campaign_defines_ordered_deploy_and_rollback_gates()
    {
        Assert.Equal("RB17 CI CD Deploy Rollback", Rb17CiCdDeployRollbackCampaign.CampaignName);
        Assert.Equal("feature/rb17-ci-cd-deploy-rollback", Rb17CiCdDeployRollbackCampaign.ReleaseCandidateBranch);

        Assert.Equal(21, Rb17CiCdDeployRollbackCampaign.Gates.Count);
        Assert.Equal("release-campaign-record", Rb17CiCdDeployRollbackCampaign.Gates[0].Name);
        Assert.Equal("post-release-handoff", Rb17CiCdDeployRollbackCampaign.Gates[^1].Name);

        Assert.Contains(Rb17CiCdDeployRollbackCampaign.Gates, gate => gate.Name == "source-ci-quality-gate" && !gate.RequiresProductionChangeWindow);
        Assert.Contains(Rb17CiCdDeployRollbackCampaign.Gates, gate => gate.Name == "canary-api-deploy" && gate.RequiresProductionChangeWindow);
        Assert.Contains(Rb17CiCdDeployRollbackCampaign.Gates, gate => gate.Name == "rollback-forward-fix" && gate.RequiresProductionChangeWindow);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_campaign_defines_required_evidence_without_duplicates()
    {
        Assert.Equal(21, Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders.Count);
        Assert.Equal(
            Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders.Count,
            Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal("01-release-campaign", Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders[0]);
        Assert.Equal("21-post-release-handoff", Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders[^1]);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_campaign_inventory_does_not_contain_secret_material()
    {
        var rendered = string.Join(
            Environment.NewLine,
            Rb17CiCdDeployRollbackCampaign.Gates.Select(gate => $"{gate.Name}|{gate.Purpose}|{gate.RequiresProductionChangeWindow}")
                .Concat(Rb17CiCdDeployRollbackCampaign.RequiredEvidenceFolders));

        SecretAssertions.DoesNotContainSecretMaterial(rendered);
    }
}
