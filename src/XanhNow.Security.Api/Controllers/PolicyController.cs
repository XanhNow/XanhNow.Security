using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Policy;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/policies")]
public sealed class PolicyController : ApiControllerBase
{
    private readonly ApplicationExecutor<EvaluateSecurityPolicyCommand, PolicyDecisionResultDto> _evaluate;

    public PolicyController(ApplicationExecutor<EvaluateSecurityPolicyCommand, PolicyDecisionResultDto> evaluate)
        => _evaluate = evaluate;

    [Authorize(Policy = SecurityPolicyNames.UserOrService)]
    [HttpPost("evaluate")]
    [EndpointMaturity("Current", "policies.evaluate")]
    public async Task<ActionResult<ApiResponse<PolicyDecisionResponse>>> EvaluateAsync(EvaluatePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _evaluate.ExecuteAsync(new EvaluateSecurityPolicyCommand(request.UserId, request.PolicyCode, request.Purpose, request.AssuranceLevel, request.Context), cancellationToken);
        return FromApplicationResult(result, x => new PolicyDecisionResponse(x.DecisionId, x.PolicyCode, MapDecision(x.Decision), x.ReasonCode, x.PolicyVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), x.EvaluatedAtUtc));
    }

    private static PolicyDecisionContract MapDecision(string decision)
        => Enum.TryParse<PolicyDecisionContract>(decision, ignoreCase: true, out var parsed) ? parsed : PolicyDecisionContract.Deny;
}
