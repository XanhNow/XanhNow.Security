using XanhNow.Security.Migrator;

namespace XanhNow.Security.Migrator.Tests;

public sealed class MigratorCommandLineTests
{
    [Fact]
    public void TryParse_DefaultsToValidate()
    {
        var parsed = MigratorCommandLine.TryParse([], out var commandLine);

        Assert.True(parsed);
        Assert.Equal(MigratorMode.Validate, commandLine.Mode);
    }

    [Theory]
    [InlineData("validate", MigratorMode.Validate)]
    [InlineData("plan", MigratorMode.Plan)]
    [InlineData("apply", MigratorMode.Apply)]
    public void TryParse_SupportsModeShortcut(string value, MigratorMode expected)
    {
        var parsed = MigratorCommandLine.TryParse([value], out var commandLine);

        Assert.True(parsed);
        Assert.Equal(expected, commandLine.Mode);
    }

    [Fact]
    public void TryParse_RejectsUnknownMode()
    {
        var parsed = MigratorCommandLine.TryParse(["seed"], out _);

        Assert.False(parsed);
    }
}
