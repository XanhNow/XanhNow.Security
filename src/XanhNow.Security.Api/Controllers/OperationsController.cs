using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/operations")]
public sealed class OperationsController : ApiControllerBase
{
    private readonly ApplicationExecutor<GetOperationStatusQuery, OperationStatusResult> _status;

    public OperationsController(ApplicationExecutor<GetOperationStatusQuery, OperationStatusResult> status)
    {
        _status = status;
    }

    [HttpGet("{operationId:guid}")]
    [EndpointMaturity("Current", "operations.status")]
    public async Task<ActionResult<ApiResponse<OperationStatusResponse>>> GetAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var result = await _status.ExecuteAsync(new GetOperationStatusQuery(CurrentUserIdOrEmpty(), operationId), cancellationToken);
        return FromApplicationResult(result, x => new OperationStatusResponse(
            x.OperationId,
            MapStatus(x.Status),
            x.OperationType,
            x.CurrentStep,
            x.ResultCode,
            x.UpdatedAtUtc));
    }

    private static OperationStatusContract MapStatus(string status)
        => status switch
        {
            "Pending" => OperationStatusContract.Accepted,
            "Validating" => OperationStatusContract.Running,
            "Running" => OperationStatusContract.Running,
            "Partial" => OperationStatusContract.Partial,
            "RetryPending" => OperationStatusContract.Partial,
            "Completed" => OperationStatusContract.Completed,
            "FailedSafe" => OperationStatusContract.FailedSafe,
            "Cancelled" => OperationStatusContract.Cancelled,
            "Expired" => OperationStatusContract.Failed,
            _ => OperationStatusContract.Failed
        };
}
