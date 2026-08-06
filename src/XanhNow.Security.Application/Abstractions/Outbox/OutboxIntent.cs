namespace XanhNow.Security.Application.Abstractions.Outbox;

public sealed record OutboxIntent(Guid EventId, string EventType, string AggregateType, Guid AggregateId, string PayloadJson, DateTimeOffset OccurredAt);

public interface IOutboxIntentWriter
{
    ValueTask AppendAsync(OutboxIntent intent, CancellationToken cancellationToken);
}


public sealed record OutboxDispatchResult(
    int Selected,
    int Processed,
    int Succeeded,
    int Failed,
    int Retried,
    int DeadLettered);

public interface ISecurityOutboxDispatcher
{
    ValueTask<OutboxDispatchResult> DispatchAsync(int batchSize, CancellationToken cancellationToken);
}
