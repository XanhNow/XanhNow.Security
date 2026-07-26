using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XanhNow.Security.Infrastructure.Persistence;
using XanhNow.Security.Migrator.Credentials;
using XanhNow.Security.Migrator.Options;
using XanhNow.Security.Migrator.Runtime;
using XanhNow.Security.Migrator.Services;

namespace XanhNow.Security.Migrator.Composition;

public static class SecurityMigratorServiceCollectionExtensions
{
    public static IServiceCollection AddXanhNowSecurityMigrator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MigratorOptions>()
            .Bind(configuration.GetSection(MigratorOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MigratorOptions>, MigratorOptionsValidator>();

        services.AddSingleton<IMigratorCredentialProvider, MigratorCredentialProvider>();
        services.AddDbContext<SecurityDbContext>((sp, options) =>
        {
            var credentialProvider = sp.GetRequiredService<IMigratorCredentialProvider>();
            var connectionString = credentialProvider.LoadConnectionStringAsync(CancellationToken.None).GetAwaiter().GetResult();
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(SecurityDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable(SecurityDatabaseConstants.MigrationHistoryTable, SecurityDatabaseConstants.Schema);
            });
        });

        services.AddScoped<ITargetPreflightService, EfTargetPreflightService>();
        services.AddScoped<IMigrationPlanner, EfMigrationPlanner>();
        services.AddScoped<IMigrationApplier, EfMigrationApplier>();
        services.AddScoped<IMigrationVerifier, EfMigrationVerifier>();
        services.AddScoped<IMigrationLockManager, PostgresAdvisoryMigrationLockManager>();
        services.AddScoped<MigrationRunner>();

        return services;
    }
}
