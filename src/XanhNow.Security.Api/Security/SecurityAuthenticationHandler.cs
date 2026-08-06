using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.Api.Security;

public sealed class SecurityAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "XanhNowSecurity";

    private readonly IJwtTokenClient _jwt;

    public SecurityAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IJwtTokenClient jwt)
        : base(options, logger, encoder)
    {
        _jwt = jwt;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var serviceName = Request.Headers["X-Service-Name"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            return AuthenticateResult.Success(CreateTicket("service", serviceName.Trim(), "service", null));
        }

        if (Request.Headers.Authorization.FirstOrDefault() is { } authorization && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            var validation = await _jwt.ValidateAsync(token, Context.RequestAborted);
            if (!validation.IsSuccess)
            {
                return AuthenticateResult.Fail(validation.Error?.Message ?? "Bearer token validation failed.");
            }

            var validatedToken = validation.Value;
            if (validatedToken is null || !validatedToken.IsValid || validatedToken.UserId is null)
            {
                return AuthenticateResult.Fail("Bearer token is invalid.");
            }

            return AuthenticateResult.Success(CreateTicket("user", validatedToken.UserId.Value.ToString("D"), "user", validatedToken.SessionId));
        }

        return AuthenticateResult.NoResult();
    }

    private static AuthenticationTicket CreateTicket(string callerType, string name, string role, string? sessionId)
    {
        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim("sub", name));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, name));
        identity.AddClaim(new Claim(ClaimTypes.Name, name));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        identity.AddClaim(new Claim("caller_type", callerType));
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            identity.AddClaim(new Claim("session_id", sessionId));
        }

        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, SchemeName);
    }
}
