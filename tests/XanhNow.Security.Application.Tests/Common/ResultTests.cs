using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_CarriesValueWithoutError()
    {
        var result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesStableError()
    {
        var error = Error.Validation(SecurityErrorCodes.ValidationFailed, "Invalid request.");

        var result = Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(SecurityErrorCodes.ValidationFailed, result.Error?.Code);
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }
}
