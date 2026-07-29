using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts;
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
    private readonly ApplicationExecutor<ProtectAccountFromTakeoverCommand, AccountStateResult> _protectTakeover;
    private readonly ApplicationExecutor<DeleteOwnAccountCommand, DeleteOwnAccountResult> _deleteOwnAccount;

    public AccountController(
        ApplicationExecutor<GetSecurityProfileQuery, SecurityProfileResult> profile,
        ApplicationExecutor<ChangeAccountStateCommand, AccountStateResult> stateChange,
        ApplicationExecutor<ProtectAccountFromTakeoverCommand, AccountStateResult> protectTakeover,
        ApplicationExecutor<DeleteOwnAccountCommand, DeleteOwnAccountResult> deleteOwnAccount)
    {
        _profile = profile;
        _stateChange = stateChange;
        _protectTakeover = protectTakeover;
        _deleteOwnAccount = deleteOwnAccount;
    }

    [HttpGet("me/security-profile")]
    [EndpointMaturity("Current", "accounts.security-profile")]
    public async Task<ActionResult<ApiResponse<SecurityProfileResponse>>> GetSecurityProfileAsync(CancellationToken cancellationToken)
    {
        var result = await _profile.ExecuteAsync(new GetSecurityProfileQuery(CurrentUserIdOrEmpty()), cancellationToken);
        return FromApplicationResult(result, x => new SecurityProfileResponse(x.UserId, x.MaskedPhoneNumber, MapStatus(x.Status), MapTrust(x.DeviceTrustLevel), x.HasPasskey, x.HasSmartOtp, x.IsStale, x.UpdatedAtUtc));
    }

    [HttpDelete("me")]
    [EndpointMaturity("Current", "accounts.delete_self")]
    public async Task<ActionResult> DeleteOwnAccountAsync(CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[SecurityHeaders.IdempotencyKey].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ProblemEnvelope(Error.Validation(SecurityErrorCodes.ValidationFailed, "Idempotency-Key header is required."));
        }

        var correlationId = Request.Headers[SecurityHeaders.CorrelationId].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return ProblemEnvelope(Error.Validation(SecurityErrorCodes.ValidationFailed, "X-Correlation-Id header is required."));
        }

        var result = await _deleteOwnAccount.ExecuteAsync(new DeleteOwnAccountCommand(CurrentUserIdOrEmpty(), idempotencyKey, correlationId, Request.Headers[SecurityHeaders.StepUpGrant].ToString()), cancellationToken);
        return result.IsSuccess ? NoContent() : ProblemEnvelope(result.Error ?? Error.Unexpected(SecurityErrorCodes.Unexpected, "Delete own account failed."));
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

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("{userId:guid}/protect-takeover")]
    [EndpointMaturity("Current", "accounts.protect_takeover")]
    public async Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> ProtectTakeoverAsync(Guid userId, AccountStateChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _protectTakeover.ExecuteAsync(new ProtectAccountFromTakeoverCommand(userId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => new AccountStateChangeResponse(x.UserId, MapStatus(x.Status), x.ChangedAtUtc));
    }

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
