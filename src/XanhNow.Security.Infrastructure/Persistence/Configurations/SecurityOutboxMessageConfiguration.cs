using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Infrastructure.Persistence.Models;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityOutboxMessageConfiguration : IEntityTypeConfiguration<SecurityOutboxMessageRow>
{
    public void Configure(EntityTypeBuilder<SecurityOutboxMessageRow> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.OutboxMessagesTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("outbox_message_id");
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(160).IsRequired();
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(160).IsRequired();
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.HeadersJson).HasColumnName("headers_json").HasColumnType("jsonb");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(512);
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("ux_security_outbox_messages_event_id");
        builder.HasIndex(x => new { x.Status, x.NextRetryAt }).HasDatabaseName("ix_security_outbox_messages_status_next_retry_at");
    }
}
