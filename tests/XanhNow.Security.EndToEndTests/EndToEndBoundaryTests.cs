using XanhNow.Security.Tests.TestKit;
using XanhNow.Security.Tests.TestKit.Fixtures;

namespace XanhNow.Security.EndToEndTests;

public sealed class EndToEndBoundaryTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.EndToEnd)]
    public void End_to_end_foundation_is_present_but_external_fixtures_are_off_by_default_in_rb11()
    {
        Environment.SetEnvironmentVariable("XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES", null);

        var root = RepositoryRoot.Find();
        var identity = TestRunIdentity.Create(root);

        Assert.Equal(ExternalFixtureMode.Disabled, ExternalFixtureModeResolver.Current());
        Assert.StartsWith(root, identity.RepositoryRoot, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(identity.RunId);
    }
}
