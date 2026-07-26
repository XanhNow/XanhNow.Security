using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.Worker.Tests;

public sealed class WorkerBoundaryTests
{
    [Fact]
    public void Worker_project_does_not_reference_api_or_contracts()
    {
        var root = FindRepositoryRoot();
        var projectFile = Path.Combine(root, "src", "XanhNow.Security.Worker", "XanhNow.Security.Worker.csproj");
        var refs = XDocument.Load(projectFile).Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension((string?)x.Attribute("Include") ?? string.Empty))
            .ToArray();

        Assert.DoesNotContain("XanhNow.Security.Api", refs);
        Assert.DoesNotContain("XanhNow.Security.Contracts", refs);
        Assert.Equal(new[] { "XanhNow.Security.Application", "XanhNow.Security.Infrastructure" }, refs.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void Background_services_do_not_inject_dbcontext_or_child_app_clients()
    {
        var forbidden = new[]
        {
            typeof(SecurityDbContext),
            typeof(IAuthLoginClient),
            typeof(IJwtTokenClient),
            typeof(IPasskeyClient),
            typeof(ISmartOtpClient)
        };

        var hostedServices = typeof(XanhNow.Security.Worker.AssemblyMarker).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(BackgroundService)));

        foreach (var service in hostedServices)
        {
            var parameters = service.GetConstructors().SelectMany(ctor => ctor.GetParameters()).Select(parameter => parameter.ParameterType);
            Assert.DoesNotContain(parameters, parameter => forbidden.Contains(parameter));
        }
    }

    [Fact]
    public void Worker_source_has_no_controllers_or_http_actions()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "XanhNow.Security.Worker"), "*.cs", SearchOption.AllDirectories);
        var source = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        Assert.DoesNotContain("Controller", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpGet", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost", source, StringComparison.Ordinal);
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

