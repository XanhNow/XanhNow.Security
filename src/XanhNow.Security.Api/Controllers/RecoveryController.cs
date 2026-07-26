using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Recovery;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/recovery")]
public sealed class RecoveryController : ApiControllerBase
{
    private readonly ApplicationExecutor<ReportLostPhoneCommand, RecoveryWorkflowResult> _reportLostPhone;
    private readonly ApplicationExecutor<StartAccountRecoveryCommand, RecoveryWorkflowResult> _startRecovery;
    private readonly ApplicationExecutor<CompleteAccountRecoveryCommand, RecoveryWorkflowResult> _completeRecovery;

    public RecoveryController(
        ApplicationExecutor<ReportLostPhoneCommand, RecoveryWorkflowResult> reportLostPhone,
        ApplicationExecutor<StartAccountRecoveryCommand, RecoveryWorkflowResult> startRecovery,
        ApplicationExecutor<CompleteAccountRecoveryCommand, RecoveryWorkflowResult> completeRecovery)
    {
        _reportLostPhone = reportLostPhone;
        _startRecovery = startRecovery;
        _completeRecovery = completeRecovery;
    }

    [HttpPost("lost-device")]
    [EndpointMaturity("Current", "recovery.lost_device")]
    public async Task<ActionResult<ApiResponse<RecoveryCaseResponse>>> ReportLostDeviceAsync(ReportLostDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _reportLostPhone.ExecuteAsync(new ReportLostPhoneCommand(request.UserId, request.DeviceReference, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => MapRecovery(x, "LostDevice"));
    }

    [HttpPost("cases")]
    [EndpointMaturity("Current", "recovery.cases.start")]
    public async Task<ActionResult<ApiResponse<RecoveryCaseResponse>>> StartRecoveryAsync(StartRecoveryCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _startRecovery.ExecuteAsync(new StartAccountRecoveryCommand(request.UserId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => MapRecovery(x, request.RecoveryType));
    }

    [HttpPost("cases/{recoveryCaseId:guid}/complete")]
    [EndpointMaturity("Current", "recovery.cases.complete")]
    public async Task<ActionResult<ApiResponse<RecoveryCaseResponse>>> CompleteRecoveryAsync(Guid recoveryCaseId, CompleteRecoveryCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _completeRecovery.ExecuteAsync(new CompleteAccountRecoveryCommand(request.UserId, recoveryCaseId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, x => MapRecovery(x, "AccountRecovery"));
    }

    private static RecoveryCaseResponse MapRecovery(RecoveryWorkflowResult result, string recoveryType)
        => new(result.RecoveryCaseId, result.UserId, recoveryType, MapStatus(result.Status), result.CurrentStep, result.UpdatedAtUtc, result.UpdatedAtUtc);

    private static OperationStatusContract MapStatus(string status)
        => status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ? OperationStatusContract.Completed : OperationStatusContract.Running;
}
