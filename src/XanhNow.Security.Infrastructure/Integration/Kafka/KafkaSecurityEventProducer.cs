namespace XanhNow.Security.Infrastructure.Integration.Kafka;

public sealed record KafkaEventEnvelope(
    Guid EventId,
    string EventType,
    string AggregateType,
    Guid AggregateId,
    string PayloadJson,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string> Headers);

public interface IKafkaSecurityEventProducer
{
    ValueTask ProduceAsync(string topic, KafkaEventEnvelope envelope, CancellationToken cancellationToken);
}

internal sealed class KafkaSecurityEventProducer : IKafkaSecurityEventProducer
{
    private readonly List<KafkaEventEnvelope> _produced = [];

    public IReadOnlyCollection<KafkaEventEnvelope> Produced => _produced.AsReadOnly();

    public ValueTask ProduceAsync(string topic, KafkaEventEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(envelope);
        _produced.Add(envelope);
        return ValueTask.CompletedTask;
    }
}
