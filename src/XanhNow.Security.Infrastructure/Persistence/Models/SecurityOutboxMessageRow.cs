using XanhNow.Security.Domain.Common;

namespace XanhNow.Security.Infrastructure.Persistence.Models;

public sealed class SecurityOutboxMessageRow : Entity<Guid>
{
    private SecurityOutboxMessageRow()
    {
    }

    public SecurityOutboxMessageRow(Guid id, Guid eventId, string eventType, string aggregateType, Guid aggregateId, string payloadJson, DateTimeOffset occurredAt)
        : base(Guard.NotEmpty(id, nameof(id)))
    {
        EventId = Guard.NotEmpty(eventId, nameof(eventId));
        EventType = Guard.NotBlank(eventType, nameof(eventType), 160);
        AggregateType = Guard.NotBlank(aggregateType, nameof(aggregateType), 160);
        AggregateId = Guard.NotEmpty(aggregateId, nameof(aggregateId));
        PayloadJson = Guard.NotBlank(payloadJson, nameof(payloadJson), 16000);
        OccurredAt = occurredAt;
        Status = "PENDING";
        CreatedAt = occurredAt;
    }

    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public string? HeadersJson { get; private set; }
    public string Status { get; private set; } = "PENDING";
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkPublishing()
    {
        Status = "PUBLISHING";
    }

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        Status = "PUBLISHED";
        PublishedAt = publishedAt;
        LastError = null;
        NextRetryAt = null;
    }

    public void MarkFailed(string error, DateTimeOffset nextRetryAt)
    {
        RetryCount++;
        Status = "FAILED";
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Kafka publish failed."
            : error.Length > 512 ? error[..512] : error;
        NextRetryAt = nextRetryAt;
    }

    public void MarkDeadLetter(string error)
    {
        RetryCount++;
        Status = "DEAD_LETTER";
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Kafka publish failed."
            : error.Length > 512 ? error[..512] : error;
        NextRetryAt = null;
    }
}
