using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace XanhNow.Security.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureLongRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<long>("RowVersion")
            .HasColumnName("row_version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken();
    }

    public static void ConfigureCreatedUpdated<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
    }
}
