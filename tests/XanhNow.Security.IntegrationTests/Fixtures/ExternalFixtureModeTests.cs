using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Fixtures;

namespace XanhNow.Security.IntegrationTests.Fixtures;

public sealed class ExternalFixtureModeTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Integration)]
    public void External_fixtures_are_disabled_by_default()
    {
        Environment.SetEnvironmentVariable("XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES", null);

        Assert.Equal(ExternalFixtureMode.Disabled, ExternalFixtureModeResolver.Current());
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Integration)]
    public void External_fixtures_can_be_enabled_explicitly_for_later_runbooks()
    {
        try
        {
            Environment.SetEnvironmentVariable("XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES", "true");

            Assert.Equal(ExternalFixtureMode.DisposableContainers, ExternalFixtureModeResolver.Current());
        }
        finally
        {
            Environment.SetEnvironmentVariable("XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES", null);
        }
    }
}
