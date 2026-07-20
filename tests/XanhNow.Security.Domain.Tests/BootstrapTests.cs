using Xunit;

namespace XanhNow.Security.Domain.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void Assembly_can_be_loaded()
    {
        Assert.Equal("XanhNow.Security.Domain", typeof(XanhNow.Security.Domain.AssemblyMarker).Assembly.GetName().Name);
    }
}
