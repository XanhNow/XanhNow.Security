using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Audit;

public sealed class SecurityAuditLog : Entity<Guid>
{
    private SecurityAuditLog()
    {
    }

    private SecurityAuditLog(Guid id, Guid? userId, AuditAction action, string outcome, ReasonCode reason, TraceId traceId, DateTimeOffset occurredAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        UserId = userId;
        Action = action;
        Outcome = Guard.NotBlank(outcome, nameof(outcome), 128);
        Reason = reason;
        TraceId = traceId;
        OccurredAt = occurredAt;
    }

    public Guid? UserId { get; private set; }
    public AuditAction Action { get; private set; } = null!;
    public string Outcome { get; private set; } = string.Empty;
    public ReasonCode Reason { get; private set; } = null!;
    public TraceId TraceId { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }

    public static SecurityAuditLog Create(Guid id, Guid? userId, AuditAction action, string outcome, ReasonCode reason, TraceId traceId, DateTimeOffset occurredAt)
        => new(id, userId, action, outcome, reason, traceId, occurredAt);
}
