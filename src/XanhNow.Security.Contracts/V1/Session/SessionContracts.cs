using XanhNow.Security.Contracts.Common.Attributes;
using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.V1.Session;

public sealed record RefreshSessionRequest([property: SensitiveData("refresh-token")] string RefreshToken, string? SessionId);
public sealed record SessionSummaryResponse(string SessionId, Guid UserId, SessionStatusContract Status, string? DeviceName, string? Platform, DateTimeOffset CreatedAtUtc, DateTimeOffset LastSeenAtUtc, DateTimeOffset ExpiresAtUtc);
public sealed record LogoutSessionRequest(string ReasonCode);
public sealed record LogoutAllSessionsRequest(string ReasonCode, bool IncludeCurrentSession);
public sealed record LogoutResponse(string SessionId, SessionStatusContract Status, DateTimeOffset RevokedAtUtc);
