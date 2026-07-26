using System.Security.Claims;
using XanhNow.Security.Application.Abstractions.Context;

namespace XanhNow.Security.Api.Security;

public sealed class HttpCurrentCaller : ICallerContextAccessor, ICorrelationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentCaller(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CallerContext Current
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http is null || http.User.Identity?.IsAuthenticated != true)
            {
                return CallerContext.Anonymous;
            }

            var callerType = http.User.FindFirstValue("caller_type") == "service" ? CallerType.Service : CallerType.EndUser;
            var subject = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var permissions = http.User.Claims.Select(c => c.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return new CallerContext(callerType, null, subject, permissions);
        }
    }

    public CorrelationContext CurrentCorrelation
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            var correlationId = http?.Items["X-Correlation-Id"]?.ToString() ?? http?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
            return new CorrelationContext(correlationId, http?.TraceIdentifier ?? correlationId);
        }
    }

    CorrelationContext ICorrelationContextAccessor.Current => CurrentCorrelation;
}
