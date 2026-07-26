using XanhNow.Security.Application.Abstractions.Policy;

namespace XanhNow.Security.Infrastructure.Integration.Policy;

internal sealed class FoundationPolicyEvaluator : IPolicyEvaluator
{
    private static readonly Guid FoundationPolicyId = Guid.Parse("10000000-0000-0000-0000-000000000014");

    public ValueTask<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Action) || string.IsNullOrWhiteSpace(context.AssuranceLevel))
        {
            return ValueTask.FromResult(PolicyEvaluationResult.Deny(FoundationPolicyId, 1, "policy_context_invalid"));
        }

        if (context.Metadata.TryGetValue("force_deny", out var forceDeny) &&
            bool.TryParse(forceDeny, out var deny) &&
            deny)
        {
            return ValueTask.FromResult(PolicyEvaluationResult.Deny(FoundationPolicyId, 1, "policy_denied"));
        }

        return ValueTask.FromResult(PolicyEvaluationResult.Allow(FoundationPolicyId, 1));
    }
}
