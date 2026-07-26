using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Contracts.Common.Errors;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiResponseMetadata CreateMetadata()
    {
        var options = HttpContext.RequestServices.GetRequiredService<IOptions<SecurityApiOptions>>().Value;
        return new ApiResponseMetadata(
            options.ContractVersion,
            HttpContext.TraceIdentifier,
            HttpContext.TraceIdentifier,
            DateTimeOffset.UtcNow);
    }

    protected ActionResult<ApiResponse<T>> OkEnvelope<T>(T data)
    {
        return Ok(new ApiResponse<T>(data, CreateMetadata()));
    }

    protected ActionResult ProblemEnvelope(Error error)
    {
        var (statusCode, apiCode) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationFailed),
            ErrorType.Authentication => (StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthenticated),
            ErrorType.Authorization => (StatusCodes.Status403Forbidden, ApiErrorCodes.Forbidden),
            ErrorType.PolicyDenied => (StatusCodes.Status403Forbidden, ApiErrorCodes.PolicyDenied),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, ApiErrorCodes.Conflict),
            ErrorType.RateLimited => (StatusCodes.Status429TooManyRequests, ApiErrorCodes.RateLimited),
            ErrorType.DownstreamUnavailable => (StatusCodes.Status503ServiceUnavailable, ApiErrorCodes.DependencyUnavailable),
            _ => (StatusCodes.Status500InternalServerError, ApiErrorCodes.Unexpected)
        };

        return StatusCode(statusCode, ApiErrorFactory.Create(HttpContext, apiCode, error.Message));
    }

    protected ActionResult<ApiResponse<TContract>> FromApplicationResult<TApplication, TContract>(
        Result<TApplication> result,
        Func<TApplication, TContract> map)
    {
        if (result.IsFailure || result.Value is null)
        {
            return ProblemEnvelope(result.Error ?? Error.Unexpected(SecurityErrorCodes.Unexpected, "Application request failed."));
        }

        return OkEnvelope(map(result.Value));
    }

    protected Guid CurrentUserIdOrEmpty()
    {
        var raw = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
    }
}
