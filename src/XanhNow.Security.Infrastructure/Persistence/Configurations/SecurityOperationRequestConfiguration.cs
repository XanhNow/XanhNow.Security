using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityOperationRequestConfiguration : IEntityTypeConfiguration<SecurityOperationRequest>
{
    public void Configure(EntityTypeBuilder<SecurityOperationRequest> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.OperationRequestsTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("operation_request_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.OperationType).HasColumnName("operation_type").HasConversion(ValueObjectConverters.OperationTypeCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasConversion(ValueObjectConverters.IdempotencyKey()).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TerminalAt).HasColumnName("terminal_at").HasColumnType("timestamp with time zone");
        builder.Property<List<OperationStepState>>("_steps")
            .HasColumnName("step_states")
            .HasColumnType("jsonb")
            .HasConversion(
                steps => OperationStepStateJsonConverter.ToJson(steps),
                json => OperationStepStateJsonConverter.FromJson(json))
            .Metadata.SetValueComparer(new ValueComparer<List<OperationStepState>>(
                (left, right) => OperationStepStateJsonConverter.ToJson(left ?? new List<OperationStepState>()) == OperationStepStateJsonConverter.ToJson(right ?? new List<OperationStepState>()),
                value => OperationStepStateJsonConverter.ToJson(value ?? new List<OperationStepState>()).GetHashCode(StringComparison.Ordinal),
                value => OperationStepStateJsonConverter.FromJson(OperationStepStateJsonConverter.ToJson(value ?? new List<OperationStepState>()))));
        builder.Ignore(x => x.Steps);
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("ux_security_operation_requests_idempotency_key");
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_security_operation_requests_user_status");
        builder.HasIndex(x => new { x.Status, x.ExpiresAt }).HasDatabaseName("ix_security_operation_requests_status_expires_at");
        builder.Ignore(x => x.DomainEvents);
    }
}

