using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Recovery;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityRecoveryCaseConfiguration : IEntityTypeConfiguration<SecurityRecoveryCase>
{
    public void Configure(EntityTypeBuilder<SecurityRecoveryCase> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.RecoveryCasesTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("recovery_case_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason_code").HasConversion(ValueObjectConverters.ReasonCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TerminalAt).HasColumnName("terminal_at").HasColumnType("timestamp with time zone");
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_security_recovery_cases_user_status");
        builder.HasIndex(x => new { x.UserId, x.Reason }).HasDatabaseName("ix_security_recovery_cases_user_reason");
        builder.Ignore(x => x.DomainEvents);
    }
}
