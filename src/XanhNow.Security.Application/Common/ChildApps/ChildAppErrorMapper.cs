using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.ChildApps;

public static class ChildAppErrorMapper
{
    public static Error ToApplicationError(ChildCallError error)
        => error.Retryable
            ? Error.DownstreamUnavailable(SecurityErrorCodes.DownstreamUnavailable, error.Message)
            : Error.Conflict(error.Code, error.Message);
}
