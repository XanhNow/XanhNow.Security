using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Infrastructure.Integration.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.AuthLogin;

internal sealed class AuthLoginRestClient : ChildAppJsonClient, IAuthLoginClient
{
    public AuthLoginRestClient(HttpClient http, SecurityIntegrationOptions options)
        : base(http, options.AuthLogin, options.ContractVersion)
    {
    }

    public ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginRegisterRequest, AuthLoginRegisterResult>("/api/v1/auth/register", request, cancellationToken);

    public ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken)
        => PostAsync<AuthLoginPasswordRequest, AuthLoginPasswordResult>("/api/v1/auth/login/password", request, cancellationToken);
}
