namespace XanhNow.Security.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
            headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
            headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
            headers.TryAdd("Cache-Control", "no-store");
            headers.TryAdd("Pragma", "no-cache");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
