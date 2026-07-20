using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityUserConfiguration : IEntityTypeConfiguration<SecurityUser>
{
    public void Configure(EntityTypeBuilder<SecurityUser> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.UsersTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("user_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.RiskLevel).HasColumnName("risk_level").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.LastReason).HasColumnName("last_reason_code").HasConversion(ValueObjectConverters.NullableReasonCode()).HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_security_users_status");
        builder.HasIndex(x => x.RiskLevel).HasDatabaseName("ix_security_users_risk_level");
        builder.Ignore(x => x.DomainEvents);
    }
}
