using Xunit;

namespace XanhNow.Security.Application.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void Assembly_can_be_loaded()
    {
        Assert.Equal("XanhNow.Security.Application", typeof(XanhNow.Security.Application.AssemblyMarker).Assembly.GetName().Name);
    }
}
