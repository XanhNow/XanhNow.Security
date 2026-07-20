using Xunit;

namespace XanhNow.Security.Api.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void Assembly_can_be_loaded()
    {
        Assert.Equal("XanhNow.Security.Api", typeof(Program).Assembly.GetName().Name);
    }
}
