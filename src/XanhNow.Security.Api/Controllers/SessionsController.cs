using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
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

    public SessionsController(
        ApplicationExecutor<RefreshSessionCommand, TokenPairResult> refresh,
        ApplicationExecutor<LogoutSessionCommand, LogoutSessionResult> logout)
    {
        _refresh = refresh;
        _logout = logout;
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EndpointMaturity("Current", "sessions.refresh")]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> RefreshAsync(RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _refresh.ExecuteAsync(new RefreshSessionCommand(CurrentUserIdOrEmpty(), request.RefreshToken, request.SessionId), cancellationToken);
        return FromApplicationResult(result, x => new TokenPairResponse(x.AccessToken, x.RefreshToken, x.AccessTokenExpiresAtUtc, x.RefreshTokenExpiresAtUtc, x.TokenType));
    }

    [HttpPost("{sessionId}/logout")]
    [EndpointMaturity("Current", "sessions.logout")]
    public async Task<ActionResult<ApiResponse<LogoutResponse>>> LogoutAsync(string sessionId, LogoutSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _logout.ExecuteAsync(new LogoutSessionCommand(CurrentUserIdOrEmpty(), sessionId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => new LogoutResponse(x.SessionId, SessionStatusContract.Revoked, x.RevokedAtUtc));
    }
}
