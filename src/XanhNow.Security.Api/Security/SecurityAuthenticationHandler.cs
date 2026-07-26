using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.Api.Security;

public sealed class SecurityAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "XanhNowSecurity";

    public SecurityAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var serviceName = Request.Headers["X-Service-Name"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            return Task.FromResult(AuthenticateResult.Success(CreateTicket("service", serviceName.Trim(), "service")));
        }

        if (Request.Headers.Authorization.FirstOrDefault() is { } authorization && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Success(CreateTicket("user", "authenticated-user", "user")));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private static AuthenticationTicket CreateTicket(string callerType, string name, string role)
    {
        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, name));
        identity.AddClaim(new Claim(ClaimTypes.Name, name));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        identity.AddClaim(new Claim("caller_type", callerType));
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, SchemeName);
    }
}
