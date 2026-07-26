using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Campaign;
using XanhNow.Security.Tests.TestKit.ChildApps;

namespace XanhNow.Security.ContractTests.ChildApps;

public sealed class Rb15ChildAppCompatibilityMatrixTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Contract)]
    public void Rb15_child_app_compatibility_matrix_matches_catalog()
    {
        var matrix = Rb15TestCampaign.ChildAppCompatibilityMatrix;

        Assert.Equal(ChildAppContractCatalog.All.Count, matrix.Count);

        foreach (var target in ChildAppContractCatalog.All)
        {
            Assert.Equal($"{target.Protocol}|{target.RuntimePort}|{target.ContractOwner}", matrix[target.AppName]);
        }

        Assert.Equal("REST|8080|auth-login", matrix["Auth_Login_App"]);
        Assert.Equal("gRPC facade|5102|jwt-refresh-token", matrix["JWT_Refresh_Token_App"]);
        Assert.Equal("gRPC + mTLS facade|5101|passkey-provider", matrix["Passkey_Provider_App"]);
        Assert.Equal("gRPC + mTLS facade|5104|smart-otp", matrix["SmartOtp_App"]);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Contract)]
    public void Rb15_protocol_matrix_keeps_login_as_rest_and_security_factor_apps_as_grpc()
    {
        var targets = ChildAppContractCatalog.All.ToDictionary(target => target.AppName, StringComparer.Ordinal);

        Assert.Equal("REST", targets["Auth_Login_App"].Protocol);
        Assert.StartsWith("gRPC", targets["JWT_Refresh_Token_App"].Protocol, StringComparison.Ordinal);
        Assert.Contains("mTLS", targets["Passkey_Provider_App"].Protocol, StringComparison.Ordinal);
        Assert.Contains("mTLS", targets["SmartOtp_App"].Protocol, StringComparison.Ordinal);
    }
}
