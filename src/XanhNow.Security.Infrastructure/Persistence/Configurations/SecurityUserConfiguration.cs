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
        builder.Property(x => x.RegistrationStatus).HasColumnName("registration_status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.RiskLevel).HasColumnName("risk_level").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.PasswordRegisteredAt).HasColumnName("password_registered_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PasskeyRegisteredAt).HasColumnName("passkey_registered_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RegistrationCompletedAt).HasColumnName("registration_completed_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RegistrationDeviceId).HasColumnName("registration_device_id").HasMaxLength(128);
        builder.Property(x => x.RegistrationPhoneNumber).HasColumnName("registration_phone_number").HasMaxLength(32);
        builder.Property(x => x.RegistrationPhoneNumberHash).HasColumnName("registration_phone_number_hash").HasMaxLength(128);
        builder.Property(x => x.LastReason).HasColumnName("last_reason_code").HasConversion(ValueObjectConverters.NullableReasonCode()).HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_security_users_status");
        builder.HasIndex(x => x.RegistrationStatus).HasDatabaseName("ix_security_users_registration_status");
        builder.HasIndex(x => x.RegistrationDeviceId).IsUnique().HasDatabaseName("ux_security_users_registration_device_id").HasFilter("registration_device_id IS NOT NULL");
        builder.HasIndex(x => x.RiskLevel).HasDatabaseName("ix_security_users_risk_level");
        builder.ToTable(x => x.HasCheckConstraint(
            "ck_security_users_registration_status",
            "registration_status IN ('PendingPasskey', 'Completed')"));
        builder.Ignore(x => x.DomainEvents);
    }
}
