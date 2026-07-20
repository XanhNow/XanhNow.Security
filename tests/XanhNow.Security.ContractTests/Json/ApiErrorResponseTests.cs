using System.Text.Json;
using XanhNow.Security.Contracts.Common.Errors;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.ContractTests.Json;

public sealed class ApiErrorResponseTests
{
    [Fact]
    public void Serializes_camel_case_and_stable_error_shape()
    {
        var value = new ApiErrorResponse(
            ApiErrorCodes.ValidationFailed,
            "Request validation failed.",
            new[] { new ApiErrorDetail("PHONE_INVALID", "Invalid.", "phoneNumber") },
            new ApiResponseMetadata("1.0", "correlation-1", "request-1", DateTimeOffset.Parse("2026-07-20T04:30:00Z")));

        var json = JsonSerializer.Serialize(value, ContractJson.Options);

        Assert.Contains("\"code\"", json);
        Assert.Contains("\"details\"", json);
        Assert.Contains("\"field\": \"phoneNumber\"", json);
        Assert.DoesNotContain("Code", json);
    }
}
