using System.Xml.Linq;
using Xunit;

namespace XanhNow.Security.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void Source_projects_follow_clean_architecture_dependency_rule()
    {
        var root = FindRepositoryRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);
        var references = projects.ToDictionary(project => Path.GetFileNameWithoutExtension(project) ?? throw new InvalidOperationException($"Cannot read project name from {project}."), ReadProjectReferences);

        Assert.Empty(references["XanhNow.Security.Domain"]);
        Assert.Empty(references["XanhNow.Security.Contracts"]);
        Assert.Equal(new[] { "XanhNow.Security.Domain" }, references["XanhNow.Security.Application"].OrderBy(x => x));
        Assert.Equal(new[] { "XanhNow.Security.Application", "XanhNow.Security.Domain" }, references["XanhNow.Security.Infrastructure"].OrderBy(x => x));
        Assert.Equal(new[] { "XanhNow.Security.Application", "XanhNow.Security.Contracts", "XanhNow.Security.Infrastructure" }, references["XanhNow.Security.Api"].OrderBy(x => x));
        Assert.Equal(new[] { "XanhNow.Security.Application", "XanhNow.Security.Infrastructure" }, references["XanhNow.Security.Worker"].OrderBy(x => x));
        Assert.Equal(new[] { "XanhNow.Security.Infrastructure" }, references["XanhNow.Security.Migrator"].OrderBy(x => x));
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

    private static string[] ReadProjectReferences(string projectFile)
    {
        var document = XDocument.Load(projectFile);
        return document.Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension((string?)x.Attribute("Include") ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x)
            .ToArray();
    }
}

