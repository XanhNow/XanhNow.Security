using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.SmartOtp;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/smart-otp")]
public sealed class SmartOtpController : ApiControllerBase
{
    private readonly ApplicationExecutor<BeginSmartOtpEnrollmentCommand, BeginSmartOtpEnrollmentResult> _beginEnrollment;
    private readonly ApplicationExecutor<ConfirmSmartOtpEnrollmentCommand, SmartOtpDeviceStateResult> _confirmEnrollment;
    private readonly ApplicationExecutor<StartStepUpCommand, StepUpChallengeResult> _startStepUp;
    private readonly ApplicationExecutor<VerifyStepUpCommand, StepUpGrantResult> _verifyStepUp;

    public SmartOtpController(
        ApplicationExecutor<BeginSmartOtpEnrollmentCommand, BeginSmartOtpEnrollmentResult> beginEnrollment,
        ApplicationExecutor<ConfirmSmartOtpEnrollmentCommand, SmartOtpDeviceStateResult> confirmEnrollment,
        ApplicationExecutor<StartStepUpCommand, StepUpChallengeResult> startStepUp,
        ApplicationExecutor<VerifyStepUpCommand, StepUpGrantResult> verifyStepUp)
    {
        _beginEnrollment = beginEnrollment;
        _confirmEnrollment = confirmEnrollment;
        _startStepUp = startStepUp;
        _verifyStepUp = verifyStepUp;
    }

    [HttpPost("devices/enroll/begin")]
    [EndpointMaturity("Current", "smart_otp.enroll.begin")]
    public async Task<ActionResult<ApiResponse<BeginSmartOtpEnrollmentResponse>>> BeginEnrollmentAsync(BeginSmartOtpEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _beginEnrollment.ExecuteAsync(new BeginSmartOtpEnrollmentCommand(CurrentUserIdOrEmpty(), request.DeviceName), cancellationToken);
        return FromApplicationResult(result, x => new BeginSmartOtpEnrollmentResponse(x.EnrollmentId, x.ProvisioningUri, x.ManualEntryKey, x.ExpiresAtUtc));
    }

    [HttpPost("devices/enroll/confirm")]
    [EndpointMaturity("Current", "smart_otp.enroll.confirm")]
    public async Task<ActionResult<ApiResponse<SmartOtpDeviceStateResponse>>> ConfirmEnrollmentAsync(ConfirmSmartOtpEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _confirmEnrollment.ExecuteAsync(new ConfirmSmartOtpEnrollmentCommand(request.EnrollmentId, request.Otp), cancellationToken);
        return FromApplicationResult(result, x => new SmartOtpDeviceStateResponse(x.DeviceId, x.IsEnabled, x.UpdatedAtUtc));
    }

    [HttpPost("step-up/start")]
    [EndpointMaturity("Current", "smart_otp.step_up.start")]
    public async Task<ActionResult<ApiResponse<StepUpChallengeResponse>>> StartStepUpAsync(StartStepUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _startStepUp.ExecuteAsync(new StartStepUpCommand(CurrentUserIdOrEmpty(), request.Purpose, request.TransactionDigest, request.ExpiresAtUtc), cancellationToken);
        return FromApplicationResult(result, x => new StepUpChallengeResponse(x.ChallengeId, x.Purpose, x.ExpiresAtUtc));
    }

    [HttpPost("step-up/verify")]
    [EndpointMaturity("Current", "smart_otp.step_up.verify")]
    public async Task<ActionResult<ApiResponse<StepUpGrantResponse>>> VerifyStepUpAsync(VerifyStepUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _verifyStepUp.ExecuteAsync(new VerifyStepUpCommand(request.ChallengeId, request.Otp), cancellationToken);
        return FromApplicationResult(result, x => new StepUpGrantResponse(x.ChallengeId, x.StepUpGrant, x.Purpose, x.ExpiresAtUtc));
    }
}
