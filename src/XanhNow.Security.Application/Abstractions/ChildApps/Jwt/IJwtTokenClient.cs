using XanhNow.Security.Application.Abstractions.ChildApps;

namespace XanhNow.Security.Application.Abstractions.ChildApps.Jwt;

public sealed record JwtIssueRequest(Guid UserId, string Audience, IReadOnlyCollection<string> Scopes);
public sealed record JwtIssueResult(string AccessToken, string RefreshTokenReference, DateTimeOffset ExpiresAt, DateTimeOffset RefreshTokenExpiresAt, string? SessionId);
public sealed record JwtRefreshRequest(Guid UserId, string RefreshTokenReference, string? SessionId);
public sealed record JwtValidateResult(bool IsValid, Guid? UserId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Scopes, string? SessionId);
public sealed record JwtRevokeRequest(Guid UserId, string SessionId);
public sealed record JwtRevokeAllRequest(Guid UserId, string ReasonCode, bool IncludeCurrentSession, string? CurrentSessionId = null);
public sealed record JwtRevokeAllResult(int RevokedCount, DateTimeOffset RevokedAtUtc);
public sealed record JwtSessionDescriptor(string SessionId, Guid UserId, string Status, string? DeviceName, string? Platform, DateTimeOffset CreatedAtUtc, DateTimeOffset LastSeenAtUtc, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenClient
{
    ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<JwtValidateResult>> ValidateAsync(string accessToken, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken);
    ValueTask<ChildCallResult<JwtRevokeAllResult>> RevokeAllSessionsAsync(JwtRevokeAllRequest request, CancellationToken cancellationToken);
}
