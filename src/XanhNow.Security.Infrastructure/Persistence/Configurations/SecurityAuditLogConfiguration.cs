using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Audit;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.AuditLogsTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("audit_log_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasConversion(ValueObjectConverters.AuditAction()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason_code").HasConversion(ValueObjectConverters.ReasonCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TraceId).HasColumnName("trace_id").HasConversion(ValueObjectConverters.TraceId()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property<string?>("EventDataJson").HasColumnName("event_data_json").HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.OccurredAt }).HasDatabaseName("ix_security_audit_logs_user_occurred_at");
        builder.HasIndex(x => x.TraceId).HasDatabaseName("ix_security_audit_logs_trace_id");
    }
}
