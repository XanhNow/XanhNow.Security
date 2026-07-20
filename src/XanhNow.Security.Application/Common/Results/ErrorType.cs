namespace XanhNow.Security.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Authentication,
    Authorization,
    Conflict,
    RateLimited,
    PolicyDenied,
    NotFound,
    DownstreamUnavailable,
    Unexpected
}
