using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Account;
using XanhNow.Security.Contracts.V1.Password;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/password")]
public sealed class PasswordController : ApiControllerBase
{
    private readonly ApplicationExecutor<ChangePasswordCommand, AccountSecurityOperationResult> _change;
    private readonly ApplicationExecutor<StartPasswordResetCommand, AccountSecurityOperationResult> _resetStart;
    private readonly ApplicationExecutor<CompletePasswordResetCommand, AccountSecurityOperationResult> _resetComplete;
    private readonly ApplicationExecutor<ForcePasswordChangeCommand, AccountStateResult> _forceChange;

    public PasswordController(
        ApplicationExecutor<ChangePasswordCommand, AccountSecurityOperationResult> change,
        ApplicationExecutor<StartPasswordResetCommand, AccountSecurityOperationResult> resetStart,
        ApplicationExecutor<CompletePasswordResetCommand, AccountSecurityOperationResult> resetComplete,
        ApplicationExecutor<ForcePasswordChangeCommand, AccountStateResult> forceChange)
    {
        _change = change;
        _resetStart = resetStart;
        _resetComplete = resetComplete;
        _forceChange = forceChange;
    }

    [HttpPost("change")]
    [EndpointMaturity("Current", "password.change")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> ChangeAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _change.ExecuteAsync(new ChangePasswordCommand(CurrentUserIdOrEmpty(), request.CurrentPassword, request.NewPassword, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    [AllowAnonymous]
    [HttpPost("reset/start")]
    [EndpointMaturity("Current", "password.reset.start")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> StartResetAsync(StartPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _resetStart.ExecuteAsync(new StartPasswordResetCommand(request.PhoneNumber), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    [AllowAnonymous]
    [HttpPost("reset/complete")]
    [EndpointMaturity("Current", "password.reset.complete")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> CompleteResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _resetComplete.ExecuteAsync(new CompletePasswordResetCommand(request.ResetOperationId, request.NewPassword), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    [Authorize(Policy = SecurityPolicyNames.Internal)]
    [HttpPost("force-change")]
    [EndpointMaturity("Current", "password.force-change")]
    public async Task<ActionResult<ApiResponse<AccountStateChangeResponse>>> ForceChangeAsync(ForcePasswordChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _forceChange.ExecuteAsync(new ForcePasswordChangeCommand(request.UserId, request.NewPassword, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => new AccountStateChangeResponse(x.UserId, MapStatus(x.Status), x.ChangedAtUtc));
    }

    private static OperationAcceptedResponse MapOperation(AccountSecurityOperationResult operation)
        => new(operation.OperationId, OperationStatusContract.Accepted, operation.OperationType, operation.CurrentStep, operation.AcceptedAtUtc);

    private static SecurityStatusContract MapStatus(string status)
        => Enum.TryParse<SecurityStatusContract>(status, ignoreCase: true, out var parsed) ? parsed : SecurityStatusContract.Active;
}
