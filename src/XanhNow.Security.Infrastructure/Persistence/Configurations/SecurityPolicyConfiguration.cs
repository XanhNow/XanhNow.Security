using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Infrastructure.Persistence.Converters;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal sealed class SecurityPolicyConfiguration : IEntityTypeConfiguration<SecurityPolicy>
{
    public void Configure(EntityTypeBuilder<SecurityPolicy> builder)
    {
        builder.ToTable(SecurityDatabaseConstants.PoliciesTable);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("policy_id");
        builder.Property(x => x.Code).HasColumnName("policy_code").HasConversion(ValueObjectConverters.PolicyCode()).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Version).HasColumnName("policy_version").IsRequired();
        builder.Property(x => x.RulesJson).HasColumnName("rules_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ActivatedAt).HasColumnName("activated_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.ConfigureLongRowVersion();
        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique().HasDatabaseName("ux_security_policies_code_version");
        builder.HasIndex(x => new { x.Code, x.Status }).HasDatabaseName("ix_security_policies_code_status");
        builder.Ignore(x => x.DomainEvents);
    }
}
