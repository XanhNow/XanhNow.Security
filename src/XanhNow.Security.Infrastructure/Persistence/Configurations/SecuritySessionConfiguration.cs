using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Sessions;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecuritySessionConfiguration : IEntityTypeConfiguration<SecuritySession>
{
    public void Configure(EntityTypeBuilder<SecuritySession> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.SessionsTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("session_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.JtiHash).HasColumnName("jti_hash").HasConversion(ValueObjectConverters.JtiHash()).HasMaxLength(160).IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.IsRevoked);
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt }).HasDatabaseName("ix_security_sessions_user_expires_at");
        builder.HasIndex(x => x.JtiHash).IsUnique().HasDatabaseName("ux_security_sessions_jti_hash");
    }
}
