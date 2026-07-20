using XanhNow.Security.Application.Abstractions.Authorization;
using XanhNow.Security.Application.Abstractions.Context;
using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Application.Abstractions.Policy;
using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Application.Abstractions.Validation;
using XanhNow.Security.Application.Common.Behaviors;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Tests.Common;

public sealed class BehaviorTests
{
    [Fact]
    public async Task ValidationBehavior_ReturnsFailureAndDoesNotCallHandler()
    {
        var behavior = new ValidationBehavior<ValidatedRequest, string>([new FailingValidator()]);
        var called = false;

        var result = await behavior.HandleAsync(new ValidatedRequest(), _ =>
        {
            called = true;
            return Task.FromResult(Result<string>.Success("ok"));
        }, CancellationToken.None);

        Assert.False(called);
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task CallerAuthenticationBehavior_RejectsAnonymousCaller()
    {
        var behavior = new CallerAuthenticationBehavior<AuthRequiredRequest, string>(new CallerAccessor(CallerContext.Anonymous));

        var result = await behavior.HandleAsync(new AuthRequiredRequest(), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.Equal(SecurityErrorCodes.CallerRequired, result.Error?.Code);
    }

    [Fact]
    public async Task AuthorizationBehavior_RejectsMissingPermission()
    {
        var caller = new CallerContext(CallerType.EndUser, Guid.NewGuid(), "user", new HashSet<string>());
        var behavior = new AuthorizationBehavior<PermissionRequest, string>(new CallerAccessor(caller), new DenyAuthorizationService());

        var result = await behavior.HandleAsync(new PermissionRequest(), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.Equal(SecurityErrorCodes.PermissionDenied, result.Error?.Code);
    }

    [Fact]
    public async Task RateLimitBehavior_RejectsDeniedDecision()
    {
        var behavior = new RateLimitBehavior<LimitedRequest, string>(new DenyRateLimitService());

        var result = await behavior.HandleAsync(new LimitedRequest(), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.Equal(SecurityErrorCodes.RateLimited, result.Error?.Code);
    }

    [Fact]
    public async Task IdempotencyBehavior_RejectsConflictingRequestHash()
    {
        var behavior = new IdempotencyBehavior<IdempotentRequest, string>(new ExistingIdempotencyStore("old-hash"), new StaticFingerprint("new-hash"));

        var result = await behavior.HandleAsync(new IdempotentRequest(), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.Equal(SecurityErrorCodes.IdempotencyConflict, result.Error?.Code);
    }

    [Fact]
    public async Task PolicyBehavior_RejectsDeniedPolicy()
    {
        var behavior = new PolicyBehavior<PolicyRequest, string>(new DenyPolicyEvaluator());

        var result = await behavior.HandleAsync(new PolicyRequest(), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);

        Assert.Equal(SecurityErrorCodes.PolicyDenied, result.Error?.Code);
    }

    private sealed record ValidatedRequest : ICommand<string>;
    private sealed record AuthRequiredRequest : ICommand<string>, IRequiresAuthenticatedCaller;
    private sealed record PermissionRequest : ICommand<string>, IRequiresPermission { public string Permission => "security.write"; }
    private sealed record LimitedRequest : ICommand<string>, IRateLimitedRequest
    {
        public string RateLimitKey => "caller:user";
        public int MaxRequests => 1;
        public TimeSpan Window => TimeSpan.FromMinutes(1);
    }
    private sealed record IdempotentRequest : ICommand<string>, IIdempotentRequest { public string IdempotencyKey => "idem-001"; }
    private sealed record PolicyRequest : ICommand<string>, IPolicyRequest
    {
        public PolicyContext ToPolicyContext() => new(Guid.NewGuid(), "login", "low", new Dictionary<string, string>());
    }

    private sealed class FailingValidator : IValidator<ValidatedRequest>
    {
        public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(ValidatedRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>([new ValidationFailure("field", SecurityErrorCodes.ValidationFailed, "Invalid.")]);
    }

    private sealed class CallerAccessor : ICallerContextAccessor
    {
        public CallerAccessor(CallerContext current) => Current = current;
        public CallerContext Current { get; }
    }

    private sealed class DenyAuthorizationService : IAuthorizationService
    {
        public ValueTask<bool> HasPermissionAsync(CallerContext caller, string permission, CancellationToken cancellationToken) => ValueTask.FromResult(false);
    }

    private sealed class DenyRateLimitService : IRateLimitService
    {
        public ValueTask<RateLimitDecision> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
            => ValueTask.FromResult(RateLimitDecision.Deny(TimeSpan.FromSeconds(30)));
    }

    private sealed class ExistingIdempotencyStore : IIdempotencyStore
    {
        private readonly string _existingHash;
        public ExistingIdempotencyStore(string existingHash) => _existingHash = existingHash;
        public ValueTask<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<IdempotencyRecord?>(new IdempotencyRecord(key, _existingHash, null, false));
        public ValueTask ReserveAsync(string key, string requestHash, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask CompleteAsync(string key, string resultJson, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class StaticFingerprint : IRequestFingerprint
    {
        private readonly string _hash;
        public StaticFingerprint(string hash) => _hash = hash;
        public string Compute(object request) => _hash;
    }

    private sealed class DenyPolicyEvaluator : IPolicyEvaluator
    {
        public ValueTask<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(PolicyEvaluationResult.Deny(Guid.NewGuid(), 1, "assurance_too_low"));
    }
}
