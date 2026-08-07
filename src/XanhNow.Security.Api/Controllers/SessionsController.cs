using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Session;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/sessions")]
public sealed class SessionsController : ApiControllerBase
{
    private readonly ApplicationExecutor<RefreshSessionCommand, TokenPairResult> _refresh;
    private readonly ApplicationExecutor<LogoutSessionCommand, LogoutSessionResult> _logout;
    private readonly ApplicationExecutor<ListSessionsQuery, IReadOnlyCollection<SessionSummaryResult>> _list;
    private readonly ApplicationExecutor<LogoutAllSessionsCommand, LogoutAllSessionsResult> _logoutAll;
    private readonly ApplicationExecutor<CompositeLogoutAllCommand, LogoutAllSessionsResult> _compositeLogoutAll;

    public SessionsController(
        ApplicationExecutor<RefreshSessionCommand, TokenPairResult> refresh,
        ApplicationExecutor<LogoutSessionCommand, LogoutSessionResult> logout,
        ApplicationExecutor<ListSessionsQuery, IReadOnlyCollection<SessionSummaryResult>> list,
        ApplicationExecutor<LogoutAllSessionsCommand, LogoutAllSessionsResult> logoutAll,
        ApplicationExecutor<CompositeLogoutAllCommand, LogoutAllSessionsResult> compositeLogoutAll)
    {
        _refresh = refresh;
        _logout = logout;
        _list = list;
        _logoutAll = logoutAll;
        _compositeLogoutAll = compositeLogoutAll;
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EndpointMaturity("Current", "sessions.refresh")]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> RefreshAsync(RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _refresh.ExecuteAsync(new RefreshSessionCommand(CurrentUserIdOrEmpty(), request.RefreshToken, request.SessionId), cancellationToken);
        return FromApplicationResult(result, x => new TokenPairResponse(x.AccessToken, x.RefreshToken, x.AccessTokenExpiresAtUtc, x.RefreshTokenExpiresAtUtc, x.SessionId, x.TokenType));
    }

    [HttpPost("{sessionId}/logout")]
    [EndpointMaturity("Current", "sessions.logout")]
    public async Task<ActionResult<ApiResponse<LogoutResponse>>> LogoutAsync(string sessionId, LogoutSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _logout.ExecuteAsync(new LogoutSessionCommand(CurrentUserIdOrEmpty(), sessionId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => new LogoutResponse(x.SessionId, SessionStatusContract.Revoked, x.RevokedAtUtc));
    }

    [HttpGet]
    [EndpointMaturity("Current", "sessions.list")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse[]>>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await _list.ExecuteAsync(new ListSessionsQuery(CurrentUserIdOrEmpty()), cancellationToken);
        return FromApplicationResult(result, sessions => sessions.Select(MapSession).ToArray());
    }

    [HttpPost("logout-all")]
    [EndpointMaturity("Current", "sessions.logout-all")]
    public async Task<ActionResult<ApiResponse<LogoutAllSessionsResponse>>> LogoutAllAsync(LogoutAllSessionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _logoutAll.ExecuteAsync(new LogoutAllSessionsCommand(CurrentUserIdOrEmpty(), request.ReasonCode, request.IncludeCurrentSession, User.FindFirst("session_id")?.Value), cancellationToken);
        return FromApplicationResult(result, x => new LogoutAllSessionsResponse(x.RevokedCount, x.RevokedAtUtc));
    }

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("{userId:guid}/composite/logout-all")]
    [EndpointMaturity("Current", "sessions.composite.logout_all")]
    public async Task<ActionResult<ApiResponse<LogoutAllSessionsResponse>>> CompositeLogoutAllAsync(Guid userId, LogoutSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _compositeLogoutAll.ExecuteAsync(new CompositeLogoutAllCommand(userId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => new LogoutAllSessionsResponse(x.RevokedCount, x.RevokedAtUtc));
    }

    private static SessionSummaryResponse MapSession(SessionSummaryResult session)
        => new(session.SessionId, session.UserId, MapSessionStatus(session.Status), session.DeviceName, session.Platform, session.CreatedAtUtc, session.LastSeenAtUtc, session.ExpiresAtUtc);

    private static SessionStatusContract MapSessionStatus(string status)
        => Enum.TryParse<SessionStatusContract>(status, ignoreCase: true, out var parsed) ? parsed : SessionStatusContract.Active;
}
