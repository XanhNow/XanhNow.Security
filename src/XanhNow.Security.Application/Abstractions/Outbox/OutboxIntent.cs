namespace XanhNow.Security.Application.Abstractions.Outbox;

public sealed record OutboxIntent(Guid EventId, string EventType, string AggregateType, Guid AggregateId, string PayloadJson, DateTimeOffset OccurredAt);

public interface IOutboxIntentWriter
{
    ValueTask AppendAsync(OutboxIntent intent, CancellationToken cancellationToken);
}
