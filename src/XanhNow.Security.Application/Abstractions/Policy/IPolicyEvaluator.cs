namespace XanhNow.Security.Application.Abstractions.Policy;

public interface IPolicyEvaluator
{
    ValueTask<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken);
}
