using XanhNow.Security.Application.Abstractions.Policy;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class PolicyBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public PolicyBehavior(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IPolicyRequest policyRequest)
        {
            var decision = await _policyEvaluator.EvaluateAsync(policyRequest.ToPolicyContext(), cancellationToken);
            if (!decision.Allowed)
            {
                return Result<TResponse>.Failure(Error.PolicyDenied(SecurityErrorCodes.PolicyDenied, decision.ReasonCode));
            }
        }

        return await next(cancellationToken);
    }
}
