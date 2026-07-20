using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace XanhNow.Security.Infrastructure.Persistence;

public sealed class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("XANHNOW_SECURITY_MIGRATION_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__SecurityDb")
            ?? throw new InvalidOperationException("Set XANHNOW_SECURITY_MIGRATION_CONNECTION for design-time migration generation.");

        var builder = new DbContextOptionsBuilder<SecurityDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable(SecurityDatabaseConstants.MigrationHistoryTable, SecurityDatabaseConstants.Schema);
        });

        return new SecurityDbContext(builder.Options);
    }
}
