using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.ValueObjects;

public sealed class ValueObjectTests
{
    [Fact]
    public void StringValueObjects_RejectBlankValues()
    {
        var ex = Assert.Throws<DomainException>(() => ReasonCode.From("   "));

        Assert.Equal("value_required", ex.Code);
    }
}

