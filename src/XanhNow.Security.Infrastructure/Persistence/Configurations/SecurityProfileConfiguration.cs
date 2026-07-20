using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Profiles;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityProfileConfiguration : IEntityTypeConfiguration<SecurityProfile>
{
    public void Configure(EntityTypeBuilder<SecurityProfile> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.ProfilesTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("user_id");
        builder.Property(x => x.PasskeyCount).HasColumnName("passkey_count").IsRequired();
        builder.Property(x => x.SmartOtpDeviceCount).HasColumnName("smart_otp_device_count").IsRequired();
        builder.Property(x => x.PasswordLoginEnabled).HasColumnName("password_login_enabled").IsRequired();
        builder.Property(x => x.IsStale).HasColumnName("is_stale").IsRequired();
        builder.Property(x => x.SnapshotAt).HasColumnName("snapshot_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => x.IsStale).HasDatabaseName("ix_security_profiles_is_stale");
    }
}
