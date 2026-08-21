using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.AdminRecovery;

namespace XanhNow.Security.Api.Controllers;

[Authorize(Policy = SecurityPolicyNames.Internal)]
[Route("api/v1/admin/recovery")]
public sealed class AdminRecoveryController : ApiControllerBase
{
    private readonly ApplicationExecutor<GetAdminRecoveryUserByPhoneQuery, AdminRecoveryUserStatusResult> _getUser;
    private readonly ApplicationExecutor<ApproveAdminAccountRecoveryCommand, AdminAccountRecoveryApprovalResult> _approve;

    public AdminRecoveryController(
        ApplicationExecutor<GetAdminRecoveryUserByPhoneQuery, AdminRecoveryUserStatusResult> getUser,
        ApplicationExecutor<ApproveAdminAccountRecoveryCommand, AdminAccountRecoveryApprovalResult> approve)
    {
        _getUser = getUser;
        _approve = approve;
    }

    [HttpGet("users")]
    [EndpointMaturity("Current", "admin.recovery.users.lookup")]
    public async Task<ActionResult<ApiResponse<AdminRecoveryUserStatusResponse>>> GetUserByPhoneAsync([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var result = await _getUser.ExecuteAsync(new GetAdminRecoveryUserByPhoneQuery(phone), cancellationToken);
        return FromApplicationResult(result, x => new AdminRecoveryUserStatusResponse(
            x.UserId,
            x.PhoneNumber,
            x.MaskedPhoneNumber,
            x.Status,
            x.PasskeyCredentialCount,
            x.SmartOtpDeviceCount,
            x.UpdatedAtUtc));
    }

    [HttpPost("requests/{requestId:guid}/approve")]
    [EndpointMaturity("Current", "admin.recovery.requests.approve")]
    public async Task<ActionResult<ApiResponse<AdminApproveRecoveryResponse>>> ApproveAsync(Guid requestId, AdminApproveRecoveryRequest request, CancellationToken cancellationToken)
    {
        if (request.RequestId != requestId)
        {
            return ProblemEnvelope(Error.Validation(SecurityErrorCodes.ValidationFailed, "Route request id must match request body id."));
        }

        var result = await _approve.ExecuteAsync(new ApproveAdminAccountRecoveryCommand(request.RequestId, request.UserId, request.PhoneNumber, request.AdminId, request.Reason), cancellationToken);
        return FromApplicationResult(result, x => new AdminApproveRecoveryResponse(x.RecoveryGrantId, x.RecoveryGrant, x.ExpiresAtUtc, x.CorrelationId));
    }
}
