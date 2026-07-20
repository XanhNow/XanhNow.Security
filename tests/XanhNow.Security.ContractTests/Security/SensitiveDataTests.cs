using System.Reflection;
using XanhNow.Security.Contracts;
using XanhNow.Security.Contracts.Common.Attributes;

namespace XanhNow.Security.ContractTests.Security;

public sealed class SensitiveDataTests
{
    [Fact]
    public void Known_sensitive_property_names_are_marked()
    {
        var sensitiveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "CurrentPassword", "NewPassword",
            "AccessToken", "RefreshToken", "Otp",
            "Credential", "PublicKeyOptions", "ProvisioningUri",
            "ManualEntryKey", "StepUpGrant"
        };

        var properties = typeof(AssemblyMarker).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(property => sensitiveNames.Contains(property.Name))
            .ToArray();

        Assert.NotEmpty(properties);
        foreach (var property in properties)
        {
            Assert.NotNull(property.GetCustomAttribute<SensitiveDataAttribute>());
        }
    }
}
