using System.Security.Claims;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.Api.Middleware;

public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityApiOptions _options;

    public CorrelationMiddleware(RequestDelegate next, IOptions<SecurityApiOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadHeader(context, SecurityHeaders.CorrelationId) ?? context.TraceIdentifier;
        var requestId = Guid.NewGuid().ToString("N");

        if (correlationId.Length > _options.MaxCorrelationIdLength)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = ApiErrorFactory.Create(context, "SECURITY_INVALID_CORRELATION_ID", "Correlation id is too long.");
            await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, response, ApiJson.SerializerOptions);
            return;
        }

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
