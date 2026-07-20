using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Audit;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Infrastructure.Persistence.Writers;

internal sealed class SecurityAuditLogWriter : ISecurityAuditLogWriter
{
    private readonly SecurityDbContext _db;

    public SecurityAuditLogWriter(SecurityDbContext db) => _db = db;

    public async ValueTask AppendAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken)
        => await _db.SecurityAuditLogs.AddAsync(auditLog, cancellationToken);
}

internal sealed class AuditIntentWriter : IAuditIntentWriter
{
    private readonly SecurityDbContext _db;

    public AuditIntentWriter(SecurityDbContext db) => _db = db;

    public async ValueTask AppendAsync(AuditIntent intent, CancellationToken cancellationToken)
    {
        var row = SecurityAuditLog.Create(
            Guid.NewGuid(),
            intent.UserId,
            AuditAction.From(intent.Action),
            intent.Outcome,
            ReasonCode.From(intent.ReasonCode),
            TraceId.From(intent.TraceId),
            intent.OccurredAt);

        await _db.SecurityAuditLogs.AddAsync(row, cancellationToken);
    }
}
