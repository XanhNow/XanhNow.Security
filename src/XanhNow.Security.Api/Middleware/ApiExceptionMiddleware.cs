using System.Text.Json;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts.Common.Errors;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API exception.");
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var options = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecurityApiOptions>>().Value;
            var metadata = new ApiResponseMetadata(options.ContractVersion, context.TraceIdentifier, context.Items["X-Request-Id"]?.ToString() ?? context.TraceIdentifier, DateTimeOffset.UtcNow);
            var response = new ApiErrorResponse("SECURITY_UNEXPECTED_ERROR", "An unexpected error occurred.", Array.Empty<ApiErrorDetail>(), metadata);
            await JsonSerializer.SerializeAsync(context.Response.Body, response, ApiJson.SerializerOptions, context.RequestAborted);
        }
    }
}
