using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Phone;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/phone")]
public sealed class PhoneController : ApiControllerBase
{
    private readonly ApplicationExecutor<StartPhoneChangeCommand, AccountSecurityOperationResult> _startChange;
    private readonly ApplicationExecutor<ConfirmPhoneChangeCommand, AccountSecurityOperationResult> _confirmChange;
    private readonly ApplicationExecutor<CancelPhoneChangeCommand, AccountSecurityOperationResult> _cancelChange;

    public PhoneController(
        ApplicationExecutor<StartPhoneChangeCommand, AccountSecurityOperationResult> startChange,
        ApplicationExecutor<ConfirmPhoneChangeCommand, AccountSecurityOperationResult> confirmChange,
        ApplicationExecutor<CancelPhoneChangeCommand, AccountSecurityOperationResult> cancelChange)
    {
        _startChange = startChange;
        _confirmChange = confirmChange;
        _cancelChange = cancelChange;
    }

    [HttpPost("change/start")]
    [EndpointMaturity("Current", "phone.change.start")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> StartChangeAsync(StartPhoneChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _startChange.ExecuteAsync(new StartPhoneChangeCommand(CurrentUserIdOrEmpty(), request.NewPhoneNumber, request.StepUpGrant, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    [HttpPost("change/confirm")]
    [EndpointMaturity("Current", "phone.change.confirm")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> ConfirmChangeAsync(ConfirmPhoneChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _confirmChange.ExecuteAsync(new ConfirmPhoneChangeCommand(CurrentUserIdOrEmpty(), request.OperationId, request.Otp), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    [HttpPost("change/cancel")]
    [EndpointMaturity("Current", "phone.change.cancel")]
    public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> CancelChangeAsync(CancelPhoneChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await _cancelChange.ExecuteAsync(new CancelPhoneChangeCommand(CurrentUserIdOrEmpty(), request.OperationId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, MapOperation);
    }

    private static OperationAcceptedResponse MapOperation(AccountSecurityOperationResult operation)
        => new(operation.OperationId, OperationStatusContract.Accepted, operation.OperationType, operation.CurrentStep, operation.AcceptedAtUtc);
}
