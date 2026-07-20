using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.Jwt;

public sealed record JwtIssueRequest(Guid UserId, string Audience, IReadOnlyCollection<string> Scopes);
public sealed record JwtIssueResult(string AccessToken, string RefreshTokenReference, DateTimeOffset ExpiresAt);
public sealed record JwtRefreshRequest(Guid UserId, string RefreshTokenReference);
public sealed record JwtRevokeRequest(Guid UserId, string SessionId);

public interface IJwtTokenClient
{
    ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken);
}
