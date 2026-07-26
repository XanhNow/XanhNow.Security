using System.Xml.Linq;
using XanhNow.Security.Tests.TestKit;

namespace XanhNow.Security.ArchitectureTests;

public sealed class TestFoundationInventoryTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Solution_contains_expected_test_projects_for_rb11()
    {
        var root = RepositoryRoot.Find();
        var document = XDocument.Load(Path.Combine(root, "XanhNow.Security.slnx"));

        var testProjects = document.Descendants()
            .Where(x => x.Name.LocalName == "Project")
            .Select(x => (string?)x.Attribute("Path"))
            .Where(x => x is not null && x.StartsWith("tests/", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(new[]
        {
            "XanhNow.Security.Api.Tests",
            "XanhNow.Security.Application.Tests",
            "XanhNow.Security.ArchitectureTests",
            "XanhNow.Security.ContractTests",
            "XanhNow.Security.Domain.Tests",
            "XanhNow.Security.EndToEndTests",
            "XanhNow.Security.IntegrationTests",
            "XanhNow.Security.Migrator.Tests",
            "XanhNow.Security.Worker.Tests"
        }, testProjects);
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Architecture)]
    public void Rb11_scripts_and_shared_testkit_exist()
    {
        var root = RepositoryRoot.Find();

        Assert.True(File.Exists(Path.Combine(root, "tests", "Directory.Build.targets")));
        Assert.True(File.Exists(Path.Combine(root, "tests", "XanhNow.Security.runsettings")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "test-fast.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "test-integration.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "test-all.ps1")));
        Assert.True(Directory.Exists(Path.Combine(root, "tests", "TestKit")));
    }
}
