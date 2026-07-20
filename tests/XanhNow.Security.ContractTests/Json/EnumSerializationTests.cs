using System.Text.Json;
using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.ContractTests.Json;

public sealed class EnumSerializationTests
{
    [Fact]
    public void Public_enum_is_serialized_as_string()
    {
        var json = JsonSerializer.Serialize(SecurityStatusContract.RecoveryRequired, ContractJson.Options);

        Assert.Equal("\"RecoveryRequired\"", json);
    }
}
