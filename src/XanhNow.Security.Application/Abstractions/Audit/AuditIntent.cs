namespace XanhNow.Security.Application.Abstractions.Audit;

public sealed record AuditIntent(Guid? UserId, string Action, string Outcome, string ReasonCode, string TraceId, DateTimeOffset OccurredAt);

public interface IAuditIntentWriter
{
    ValueTask AppendAsync(AuditIntent intent, CancellationToken cancellationToken);
}
