using System.Reflection;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.ContractTests.Boundary;

public sealed class ContractsBoundaryTests
{
    [Fact]
    public void Contracts_does_not_reference_forbidden_XanhNow_projects()
    {
        var forbidden = new[]
        {
            "XanhNow.Security.Domain",
            "XanhNow.Security.Application",
            "XanhNow.Security.Infrastructure",
            "XanhNow.Security.Api",
            "XanhNow.Security.Worker",
            "XanhNow.Security.Migrator",
            "Auth_Login_App",
            "JWT_Refresh_Token_App",
            "Passkey_Provider_App",
            "SmartOtp_App"
        };

        var references = typeof(AssemblyMarker).Assembly.GetReferencedAssemblies()
            .Select(x => x.Name ?? string.Empty)
            .ToArray();

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, references);
        }
    }

    [Fact]
    public void Public_contract_types_do_not_expose_forbidden_types()
    {
        var forbiddenPrefixes = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Grpc.",
            "XanhNow.Security.Domain",
            "XanhNow.Security.Application",
            "XanhNow.Security.Infrastructure"
        };

        foreach (var type in typeof(AssemblyMarker).Assembly.GetExportedTypes())
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var fullName = property.PropertyType.FullName ?? string.Empty;
                Assert.DoesNotContain(forbiddenPrefixes, prefix => fullName.StartsWith(prefix, StringComparison.Ordinal));
            }
    }
}
