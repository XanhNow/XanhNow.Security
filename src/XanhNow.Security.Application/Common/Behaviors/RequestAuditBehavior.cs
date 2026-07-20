using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.Context;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class RequestAuditBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly ICallerContextAccessor _callerContext;
    private readonly ICorrelationContextAccessor _correlationContext;
    private readonly IAuditIntentWriter _auditIntentWriter;
    private readonly IClock _clock;

    public RequestAuditBehavior(ICallerContextAccessor callerContext, ICorrelationContextAccessor correlationContext, IAuditIntentWriter auditIntentWriter, IClock clock)
    {
        _callerContext = callerContext;
        _correlationContext = correlationContext;
        _auditIntentWriter = auditIntentWriter;
        _clock = clock;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        if (request is IAuditableRequest auditable)
        {
            var outcome = result.IsSuccess ? "success" : "failure";
            var reason = result.Error?.Code ?? "application_success";
            await _auditIntentWriter.AppendAsync(
                new AuditIntent(_callerContext.Current.UserId, auditable.AuditAction, outcome, reason, _correlationContext.Current.TraceId, _clock.UtcNow),
                cancellationToken);
        }

        return result;
    }
}
