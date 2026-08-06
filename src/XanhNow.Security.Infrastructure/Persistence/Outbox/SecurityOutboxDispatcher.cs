using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Infrastructure.Integration.Kafka;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Persistence.Outbox;

internal sealed class SecurityOutboxDispatcher : ISecurityOutboxDispatcher
{
    private const int MaxAttempts = 5;
    private readonly SecurityDbContext _db;
    private readonly IKafkaSecurityEventProducer _producer;
    private readonly SecurityIntegrationOptions _options;

    public SecurityOutboxDispatcher(SecurityDbContext db, IKafkaSecurityEventProducer producer, SecurityIntegrationOptions options)
    {
        _db = db;
        _producer = producer;
        _options = options;
    }

    public async ValueTask<OutboxDispatchResult> DispatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var messages = await _db.SecurityOutboxMessages
            .Where(x => (x.Status == "PENDING" || x.Status == "FAILED") && (x.NextRetryAt == null || x.NextRetryAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(cancellationToken);

        var succeeded = 0;
        var failed = 0;
        var retried = 0;
        var deadLettered = 0;

        foreach (var message in messages)
        {
            try
            {
                var envelope = new KafkaEventEnvelope(
                    message.EventId,
                    message.EventType,
                    message.AggregateType,
                    message.AggregateId,
                    message.PayloadJson,
                    message.OccurredAt,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["producer"] = "xanhnow-security"
                    });

                await _producer.ProduceAsync(_options.Kafka.SecurityEventsTopic, envelope, cancellationToken);
                message.MarkPublished(DateTimeOffset.UtcNow);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                if (message.RetryCount + 1 >= MaxAttempts)
                {
                    message.MarkDeadLetter(ex.Message);
                    deadLettered++;
                }
                else
                {
                    message.MarkFailed(ex.Message, DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, (message.RetryCount + 1) * 10)));
                    retried++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new OutboxDispatchResult(messages.Count, messages.Count, succeeded, failed, retried, deadLettered);
    }
}
