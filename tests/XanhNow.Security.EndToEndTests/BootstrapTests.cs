using Xunit;

namespace XanhNow.Security.EndToEndTests;

public sealed class BootstrapTests
{
    [Fact]
    public void Assembly_can_be_loaded()
    {
        Assert.Equal("XanhNow.Security.Contracts", typeof(XanhNow.Security.Contracts.AssemblyMarker).Assembly.GetName().Name);
    }
}
