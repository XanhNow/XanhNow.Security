using Microsoft.Extensions.Options;
using XanhNow.Security.Migrator.Options;

namespace XanhNow.Security.Migrator.Tests;

public sealed class MigratorOptionsValidatorTests
{
    private readonly MigratorOptionsValidator _validator = new();

    [Fact]
    public void Validate_AcceptsExpectedSecuritySchema()
    {
        var result = _validator.Validate(null, new MigratorOptions
        {
            ExpectedSchema = "security",
            ExpectedDatabase = "authtest",
            ExpectedRole = "xanhnow_security_migrator",
            LockKey = "xanhnow.security.migrator",
            Credential = new MigratorCredentialOptions { Provider = "Environment", EnvVarName = "SECURITY_MIGRATOR_CONNECTION_STRING" }
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_RejectsNonSecuritySchema()
    {
        var result = _validator.Validate(null, new MigratorOptions
        {
            ExpectedSchema = "public",
            ExpectedDatabase = "authtest",
            ExpectedRole = "xanhnow_security_migrator",
            LockKey = "xanhnow.security.migrator",
            Credential = new MigratorCredentialOptions { Provider = "Environment", EnvVarName = "SECURITY_MIGRATOR_CONNECTION_STRING" }
        });

        Assert.True(result.Failed);
    }
}
