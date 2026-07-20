using Xunit;

namespace XanhNow.Security.IntegrationTests;

public sealed class BootstrapTests
{
    [Fact]
    public void Assembly_can_be_loaded()
    {
        Assert.Equal("XanhNow.Security.Infrastructure", typeof(XanhNow.Security.Infrastructure.AssemblyMarker).Assembly.GetName().Name);
    }
}
