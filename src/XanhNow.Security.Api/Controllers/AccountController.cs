using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Account;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/accounts")]
public sealed class AccountController : ApiControllerBase
{
    private readonly ApplicationExecutor<GetSecurityProfileQuery, SecurityProfileResult> _profile;
    private readonly ApplicationExecutor<ChangeAccountStateCommand, AccountStateResult> _stateChange;

    public AccountController(
        ApplicationExecutor<GetSecurityProfileQuery, SecurityProfileResult> profile,
        ApplicationExecutor<ChangeAccountStateCommand, AccountStateResult> stateChange)
    {
        _profile = profile;
        _stateChange = stateChange;
    }

    [HttpGet("me/security-profile")]
    [EndpointMaturity("Current", "accounts.security-profile")]
    public async Task<ActionResult<ApiResponse<SecurityProfileResponse>>> GetSecurityProfileAsync(CancellationToken cancellationToken)
    {
        var result = await _profile.ExecuteAsync(new GetSecurityProfileQuery(CurrentUserIdOrEmpty()), cancellationToken);
        return FromApplicationResult(result, x => new SecurityProfileResponse(x.UserId, x.MaskedPhoneNumber, MapStatus(x.Status), MapTrust(x.DeviceTrustLevel), x.HasPasskey, x.HasSmartOtp, x.IsStale, x.UpdatedAtUtc));
    }

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("{userId:guid}/lock")]
    [EndpointMaturity("Current", "accounts.lock")]
    public Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> LockAsync(Guid userId, AccountStateChangeRequest request, CancellationToken cancellationToken)
        => ChangeStateAsync(userId, AccountStateTargetState.Locked, request, cancellationToken);

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("{userId:guid}/unlock")]
    [EndpointMaturity("Current", "accounts.unlock")]
    public Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> UnlockAsync(Guid userId, AccountStateChangeRequest request, CancellationToken cancellationToken)
        => ChangeStateAsync(userId, AccountStateTargetState.Active, request, cancellationToken);

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("{userId:guid}/disable")]
    [EndpointMaturity("Current", "accounts.disable")]
    public Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> DisableAsync(Guid userId, AccountStateChangeRequest request, CancellationToken cancellationToken)
        => ChangeStateAsync(userId, AccountStateTargetState.Disabled, request, cancellationToken);

    private async Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> ChangeStateAsync(Guid userId, AccountStateTargetState targetState, AccountStateChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _stateChange.ExecuteAsync(new ChangeAccountStateCommand(userId, targetState, request.ReasonCode, request.Comment), cancellationToken);
        return FromApplicationResult(result, x => new AccountStateChangeResponse(x.UserId, MapStatus(x.Status), x.ChangedAtUtc));
    }

    private static SecurityStatusContract MapStatus(string status)
        => Enum.TryParse<SecurityStatusContract>(status, ignoreCase: true, out var parsed) ? parsed : SecurityStatusContract.Active;

    private static DeviceTrustLevelContract MapTrust(string trustLevel)
        => Enum.TryParse<DeviceTrustLevelContract>(trustLevel, ignoreCase: true, out var parsed) ? parsed : DeviceTrustLevelContract.Unknown;
}
