using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Infrastructure.Integration.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.Jwt;

internal sealed class JwtTokenGrpcFacadeClient : ChildAppJsonClient, IJwtTokenClient
{
    public JwtTokenGrpcFacadeClient(HttpClient http, SecurityIntegrationOptions options)
        : base(http, options.Jwt, options.ContractVersion)
    {
    }

    public ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken)
        => PostAsync<JwtIssueRequest, JwtIssueResult>("/internal/v1/jwt/issue", request, cancellationToken);

    public ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken)
        => PostAsync<JwtRefreshRequest, JwtIssueResult>("/internal/v1/jwt/refresh", request, cancellationToken);

    public ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken)
        => PostAsync<JwtRevokeRequest, bool>("/internal/v1/jwt/sessions/revoke", request, cancellationToken);

    public ValueTask<ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => GetAsync<IReadOnlyCollection<JwtSessionDescriptor>>($"/internal/v1/jwt/users/{userId}/sessions", cancellationToken);

    public ValueTask<ChildCallResult<JwtRevokeAllResult>> RevokeAllSessionsAsync(JwtRevokeAllRequest request, CancellationToken cancellationToken)
        => PostAsync<JwtRevokeAllRequest, JwtRevokeAllResult>("/internal/v1/jwt/sessions/revoke-all", request, cancellationToken);
}
