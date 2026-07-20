using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Infrastructure.Persistence.Models;

namespace XanhNow.Security.Infrastructure.Persistence.Writers;

internal sealed class OutboxIntentWriter : IOutboxIntentWriter
{
    private readonly SecurityDbContext _db;

    public OutboxIntentWriter(SecurityDbContext db) => _db = db;

    public async ValueTask AppendAsync(OutboxIntent intent, CancellationToken cancellationToken)
    {
        var row = new SecurityOutboxMessageRow(
            Guid.NewGuid(),
            intent.EventId,
            intent.EventType,
            intent.AggregateType,
            intent.AggregateId,
            intent.PayloadJson,
            intent.OccurredAt);

        await _db.SecurityOutboxMessages.AddAsync(row, cancellationToken);
    }
}
