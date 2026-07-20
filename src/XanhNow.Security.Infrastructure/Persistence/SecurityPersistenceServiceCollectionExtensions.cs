using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Infrastructure.Persistence.Repositories;
using XanhNow.Security.Infrastructure.Persistence.Transactions;
using XanhNow.Security.Infrastructure.Persistence.Writers;

namespace XanhNow.Security.Infrastructure.Persistence;

public static class SecurityPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityPersistence(this IServiceCollection services, Action<SecurityPersistenceOptions> configure)
    {
        services.Configure(configure);
        services.AddDbContext<SecurityDbContext>((sp, options) =>
        {
            var persistence = sp.GetRequiredService<IOptions<SecurityPersistenceOptions>>().Value;
            if (string.IsNullOrWhiteSpace(persistence.ConnectionString))
            {
                throw new InvalidOperationException("XanhNow.Security PostgreSQL connection string is required for persistence.");
            }

            options.UseNpgsql(persistence.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(SecurityDatabaseConstants.MigrationHistoryTable, SecurityDatabaseConstants.Schema);
            });

            if (persistence.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            if (persistence.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<ISecurityUserRepository, SecurityUserRepository>();
        services.AddScoped<ISecurityGrantRepository, SecurityGrantRepository>();
        services.AddScoped<ISecurityPolicyRepository, SecurityPolicyRepository>();
        services.AddScoped<ISecurityRecoveryCaseRepository, SecurityRecoveryCaseRepository>();
        services.AddScoped<ISecurityOperationRepository, SecurityOperationRepository>();
        services.AddScoped<ISecurityProfileReader, SecurityProfileReader>();
        services.AddScoped<ISecuritySessionReader, SecuritySessionReader>();
        services.AddScoped<ISecurityPolicyDecisionWriter, SecurityPolicyDecisionWriter>();
        services.AddScoped<ISecurityAuditLogWriter, SecurityAuditLogWriter>();
        services.AddScoped<IAuditIntentWriter, AuditIntentWriter>();
        services.AddScoped<IOutboxIntentWriter, OutboxIntentWriter>();
        services.AddScoped<ILocalUnitOfWork, LocalUnitOfWork>();
        return services;
    }
}
