using System.Security.Claims;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.Api.Middleware;

public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadHeader(context, SecurityHeaders.CorrelationId) ?? context.TraceIdentifier;
        var requestId = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Items[SecurityHeaders.CorrelationId] = correlationId;
        context.Items[SecurityHeaders.RequestId] = requestId;
        context.Response.Headers[SecurityHeaders.CorrelationId] = correlationId;
        context.Response.Headers[SecurityHeaders.RequestId] = requestId;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationMiddleware>>().BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = requestId,
            ["Caller"] = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"
        }))
        {
            await _next(context);
        }
    }

    private static string? ReadHeader(HttpContext context, string name)
    {
        var value = context.Request.Headers[name].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
