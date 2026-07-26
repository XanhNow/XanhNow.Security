using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts.Common.Errors;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Api;

public static class ApiErrorFactory
{
    public static ApiErrorResponse Create(HttpContext context, string code, string message, IReadOnlyCollection<ApiErrorDetail>? details = null)
    {
        var options = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecurityApiOptions>>().Value;
        var requestId = context.Items["X-Request-Id"]?.ToString() ?? context.TraceIdentifier;
        var metadata = new ApiResponseMetadata(options.ContractVersion, context.TraceIdentifier, requestId, DateTimeOffset.UtcNow);
        return new ApiErrorResponse(code, message, details ?? Array.Empty<ApiErrorDetail>(), metadata);
    }
}
