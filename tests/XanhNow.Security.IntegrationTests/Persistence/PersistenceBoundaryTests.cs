using System.Xml.Linq;

namespace XanhNow.Security.IntegrationTests.Persistence;

public sealed class PersistenceBoundaryTests
{
    [Fact]
    public void Domain_application_and_contracts_do_not_reference_ef_or_npgsql()
    {
        var root = FindRepositoryRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(x => x.Contains("Domain") || x.Contains("Application") || x.Contains("Contracts"));

        foreach (var project in projects)
        {
            var xml = XDocument.Load(project);
            var packages = xml.Descendants().Where(x => x.Name.LocalName == "PackageReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();

            Assert.DoesNotContain(packages, x => x.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(packages, x => x.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (Directory.Exists(Path.Combine(current, "src")) && Directory.Exists(Path.Combine(current, "tests")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Cannot find repository root.");
    }
}
