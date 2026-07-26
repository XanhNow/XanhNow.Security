using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.ChildApps;

namespace XanhNow.Security.ContractTests.ChildApps;

public sealed class ChildAppContractHarnessTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Contract)]
    public void Contract_harness_tracks_all_four_child_apps_by_full_name()
    {
        var targets = ChildAppContractCatalog.All;

        Assert.Equal(new[]
        {
            "Auth_Login_App",
            "JWT_Refresh_Token_App",
            "Passkey_Provider_App",
            "SmartOtp_App"
        }, targets.Select(x => x.AppName).ToArray());

        Assert.Contains(targets, x => x.Protocol == "REST");
        Assert.Equal(3, targets.Count(x => x.Protocol.Contains("gRPC", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Contract)]
    public void Contract_harness_does_not_store_runtime_secret_material()
    {
        var rendered = string.Join(Environment.NewLine, ChildAppContractCatalog.All.Select(x => $"{x.AppName}|{x.Protocol}|{x.RuntimePort}|{x.ContractOwner}"));

        SecretAssertions.DoesNotContainSecretMaterial(rendered);
    }
}
