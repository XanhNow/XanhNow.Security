using System.Text;
using Confluent.Kafka;
using XanhNow.Security.Infrastructure.Integration.Options;
using XanhNow.Security.Infrastructure.Integration.Vault;

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

internal sealed class KafkaSecurityEventProducer : IKafkaSecurityEventProducer, IDisposable
{
    private readonly SecurityIntegrationOptions _options;
    private readonly IVaultSecretReader _secrets;
    private readonly object _gate = new();
    private IProducer<string, string>? _producer;

    public KafkaSecurityEventProducer(SecurityIntegrationOptions options, IVaultSecretReader secrets)
    {
        _options = options;
        _secrets = secrets;
    }

    public async ValueTask ProduceAsync(string topic, KafkaEventEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!string.Equals(_options.Kafka.Mode, "Kafka", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var producer = await GetProducerAsync(cancellationToken);
        var message = new Message<string, string>
        {
            Key = envelope.AggregIdAsKey(),
            Value = envelope.PayloadJson,
            Headers = BuildHeaders(envelope)
        };

        await producer.ProduceAsync(topic, message, cancellationToken);
    }

    private async ValueTask<IProducer<string, string>> GetProducerAsync(CancellationToken cancellationToken)
    {
        if (_producer is not null)
        {
            return _producer;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _options.Kafka.BootstrapServers,
            ClientId = _options.Kafka.ClientId,
            Acks = ParseAcks(_options.Kafka.Acks),
            EnableIdempotence = _options.Kafka.EnableIdempotentProducer
        };

        if (!string.IsNullOrWhiteSpace(_options.Kafka.SecretPath))
        {
            var username = await ReadOptionalAsync(_options.Kafka.SecretPath, _options.Kafka.UsernameField, cancellationToken);
            var password = await ReadOptionalAsync(_options.Kafka.SecretPath, _options.Kafka.PasswordField, cancellationToken);
            var securityProtocol = await ReadOptionalAsync(_options.Kafka.SecretPath, _options.Kafka.SecurityProtocolField, cancellationToken);
            var saslMechanism = await ReadOptionalAsync(_options.Kafka.SecretPath, _options.Kafka.SaslMechanismField, cancellationToken);

            if (!string.IsNullOrWhiteSpace(securityProtocol))
            {
                config.SecurityProtocol = Enum.Parse<SecurityProtocol>(securityProtocol, ignoreCase: true);
            }

            if (!string.IsNullOrWhiteSpace(saslMechanism))
            {
                config.SaslMechanism = Enum.Parse<SaslMechanism>(saslMechanism, ignoreCase: true);
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                config.SaslUsername = username;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                config.SaslPassword = password;
            }
        }

        lock (_gate)
        {
            _producer ??= new ProducerBuilder<string, string>(config).Build();
            return _producer;
        }
    }

    private async ValueTask<string?> ReadOptionalAsync(string path, string field, CancellationToken cancellationToken)
    {
        var value = await _secrets.ReadFieldAsync(new VaultSecretReference(path, field), cancellationToken);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private static Acks ParseAcks(string value) =>
        string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)
            ? Acks.All
            : Enum.Parse<Acks>(value, ignoreCase: true);

    private static Headers BuildHeaders(KafkaEventEnvelope envelope)
    {
        var headers = new Headers
        {
            { "event-id", Encoding.UTF8.GetBytes(envelope.EventId.ToString("N")) },
            { "event-type", Encoding.UTF8.GetBytes(envelope.EventType) },
            { "aggregate-type", Encoding.UTF8.GetBytes(envelope.AggregateType) },
            { "occurred-at", Encoding.UTF8.GetBytes(envelope.OccurredAt.ToString("O")) }
        };

        foreach (var item in envelope.Headers)
        {
            if (!string.IsNullOrWhiteSpace(item.Key) && item.Value is not null)
            {
                headers.Add(item.Key, Encoding.UTF8.GetBytes(item.Value));
            }
        }

        return headers;
    }

    public void Dispose()
    {
        if (_producer is null)
        {
            return;
        }

        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

file static class KafkaEventEnvelopeExtensions
{
    public static string AggregIdAsKey(this KafkaEventEnvelope envelope) => envelope.AggregateId.ToString("N");
}
