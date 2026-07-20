using System.Net;
using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Infrastructure.Integration.Common;

internal static class DownstreamErrorMapper
{
    public static ChildCallError FromHttpStatus(HttpStatusCode statusCode, string downstreamName)
    {
        var code = statusCode switch
        {
            HttpStatusCode.BadRequest => "downstream.bad_request",
            HttpStatusCode.Unauthorized => "downstream.unauthorized",
            HttpStatusCode.Forbidden => "downstream.forbidden",
            HttpStatusCode.NotFound => "downstream.not_found",
            HttpStatusCode.Conflict => "downstream.conflict",
            HttpStatusCode.TooManyRequests => "downstream.rate_limited",
            HttpStatusCode.RequestTimeout => "downstream.timeout",
            >= HttpStatusCode.InternalServerError => "downstream.unavailable",
            _ => "downstream.error"
        };

        return new ChildCallError(code, $"{downstreamName} returned {(int)statusCode}.", IsRetryable(statusCode));
    }

    public static ChildCallError FromException(Exception exception, string downstreamName)
        => new("downstream.transport_error", $"{downstreamName} transport error: {Redaction.Safe(exception.GetType().Name)}.", true);

    private static bool IsRetryable(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || statusCode >= HttpStatusCode.InternalServerError;
}
