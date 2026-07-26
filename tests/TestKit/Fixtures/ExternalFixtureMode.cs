namespace XanhNow.Security.Tests.TestKit.Fixtures;

public enum ExternalFixtureMode
{
    Disabled,
    DisposableContainers
}

public static class ExternalFixtureModeResolver
{
    public static ExternalFixtureMode Current()
    {
        var enabled = Environment.GetEnvironmentVariable("XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES");
        return string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)
            ? ExternalFixtureMode.DisposableContainers
            : ExternalFixtureMode.Disabled;
    }
}
