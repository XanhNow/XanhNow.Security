using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityGrantConfiguration : IEntityTypeConfiguration<SecurityGrant>
{
    public void Configure(EntityTypeBuilder<SecurityGrant> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.GrantsTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("grant_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("grant_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.Audience).HasColumnName("audience").HasConversion(ValueObjectConverters.GrantAudience()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasConversion(ValueObjectConverters.GrantPurpose()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TerminalAt).HasColumnName("terminal_at").HasColumnType("timestamp with time zone");
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_security_grants_user_status");
        builder.HasIndex(x => new { x.Type, x.Status }).HasDatabaseName("ix_security_grants_type_status");
        builder.Ignore(x => x.DomainEvents);
    }
}
