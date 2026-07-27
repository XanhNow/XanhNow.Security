using XanhNow.Security.Tests.TestKit;

namespace XanhNow.Security.ArchitectureTests;

public sealed class Rb17CiCdDeployAssetTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_defines_source_ci_release_and_deploy_assets()
    {
        var root = RepositoryRoot.Find();
        var requiredFiles = new[]
        {
            ".github/workflows/source-ci.yml",
            "scripts/build-release.ps1",
            "scripts/validate-release-bundle.ps1",
            "scripts/test-rb17.ps1",
            "deploy/xanhnow-security/inventory.production.example.json",
            "deploy/xanhnow-security/systemd/xanhnow-security-api.service",
            "deploy/xanhnow-security/systemd/xanhnow-security-worker.service",
            "deploy/xanhnow-security/deploy-api-node.sh",
            "deploy/xanhnow-security/deploy-worker-node.sh",
            "deploy/xanhnow-security/rollback-node.sh",
            "deploy/xanhnow-security/README.md"
        };

        foreach (var file in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, file)), $"Missing RB17 file: {file}");
        }
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_deploy_assets_target_only_xanhnow_security()
    {
        var root = RepositoryRoot.Find();
        var deployRoot = Path.Combine(root, "deploy", "xanhnow-security");
        var executableAssets = Directory.GetFiles(deployRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("README.md", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText);
        var rendered = string.Join(Environment.NewLine, executableAssets);

        Assert.Contains("XanhNow.Security", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Login_App", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JWT_Refresh_Token_App", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Passkey_Provider_App", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SmartOtp_App", rendered, StringComparison.OrdinalIgnoreCase);
        SecretAssertions.DoesNotContainSecretMaterial(rendered);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb17_scripts_preserve_release_manifest_and_rollback_boundary()
    {
        var root = RepositoryRoot.Find();
        var buildRelease = File.ReadAllText(Path.Combine(root, "scripts", "build-release.ps1"));
        var validateRelease = File.ReadAllText(Path.Combine(root, "scripts", "validate-release-bundle.ps1"));
        var rollback = File.ReadAllText(Path.Combine(root, "deploy", "xanhnow-security", "rollback-node.sh"));

        Assert.Contains("release.json", buildRelease, StringComparison.Ordinal);
        Assert.Contains("SHA256SUMS.txt", buildRelease, StringComparison.Ordinal);
        Assert.Contains("XanhNow.Security.Api.csproj", buildRelease, StringComparison.Ordinal);
        Assert.Contains("XanhNow.Security.Worker.csproj", buildRelease, StringComparison.Ordinal);
        Assert.Contains("XanhNow.Security.Migrator.csproj", buildRelease, StringComparison.Ordinal);
        Assert.Contains("Checksum mismatch", validateRelease, StringComparison.Ordinal);
        Assert.Contains("set -Eeuo pipefail", rollback, StringComparison.Ordinal);
        Assert.Contains("ln -sfn", rollback, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet ef database update 0", rollback, StringComparison.OrdinalIgnoreCase);
    }
}

