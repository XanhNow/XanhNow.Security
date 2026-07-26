using System.Xml.Linq;

namespace XanhNow.Security.Migrator.Tests;

public sealed class MigratorBoundaryTests
{
    [Fact]
    public void MigratorProjectReferencesOnlyInfrastructureProject()
    {
        var root = FindRepositoryRoot();
        var project = XDocument.Load(Path.Combine(root.FullName, "src", "XanhNow.Security.Migrator", "XanhNow.Security.Migrator.csproj"));
        var references = project.Descendants("ProjectReference").Select(x => x.Attribute("Include")?.Value).Where(x => x is not null).ToArray();

        Assert.Single(references);
        Assert.Contains("XanhNow.Security.Infrastructure", references[0]);
    }

    [Fact]
    public void MigratorSourceDoesNotRegisterApiWorkerOrChildAppRuntime()
    {
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root.FullName, "src", "XanhNow.Security.Migrator");
        var source = string.Join("\n", Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("AddControllers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHostedService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddGrpcClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddStackExchangeRedis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Producer", source, StringComparison.Ordinal);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "XanhNow.Security.slnx")))
        {
            current = current.Parent;
        }

        return current ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
