using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityPolicyDecisionConfiguration : IEntityTypeConfiguration<SecurityPolicyDecision>
{
    public void Configure(EntityTypeBuilder<SecurityPolicyDecision> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.PolicyDecisionsTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("decision_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.PolicyCode).HasColumnName("policy_code").HasConversion(ValueObjectConverters.PolicyCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PolicyVersion).HasColumnName("policy_version").IsRequired();
        builder.Property(x => x.Result).HasColumnName("result").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason_code").HasConversion(ValueObjectConverters.ReasonCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property<Guid?>("OperationRequestId").HasColumnName("operation_request_id");
        builder.Property<string?>("CorrelationId").HasColumnName("correlation_id").HasMaxLength(128);
        builder.HasIndex(x => new { x.UserId, x.DecidedAt }).HasDatabaseName("ix_security_policy_decisions_user_decided_at");
        builder.HasIndex(x => new { x.PolicyCode, x.DecidedAt }).HasDatabaseName("ix_security_policy_decisions_policy_decided_at");
        builder.HasIndex("OperationRequestId").HasDatabaseName("ix_security_policy_decisions_operation_request_id");
        builder.HasIndex("CorrelationId").HasDatabaseName("ix_security_policy_decisions_correlation_id");
    }
}
