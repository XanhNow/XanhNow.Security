using Microsoft.Extensions.Options;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Background;
using XanhNow.Security.Application.Background.Commands;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Infrastructure.Persistence;
using XanhNow.Security.Worker.Jobs;
using XanhNow.Security.Worker.Options;
using XanhNow.Security.Worker.Runtime;
using XanhNow.Security.Worker.Scheduling;

namespace XanhNow.Security.Worker.Composition;

public static class SecurityWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddXanhNowSecurityWorker(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<SecurityWorkerOptions>()
            .Bind(configuration.GetSection(SecurityWorkerOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityWorkerOptions>, SecurityWorkerOptionsValidator>();

        services.AddSecurityPersistence(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("SecurityDb") ?? configuration["SecurityPersistence:ConnectionString"];
            options.EnableDetailedErrors = environment.IsDevelopment();
            options.EnableSensitiveDataLogging = false;
        });

        services.AddSingleton<IClock, WorkerSystemClock>();
        services.AddScoped<IRequestHandler<RunBackgroundJobCommand, BackgroundCommandResult>, RunBackgroundJobCommandHandler>();
        services.AddScoped<ApplicationExecutor<RunBackgroundJobCommand, BackgroundCommandResult>>();

        services.AddSingleton<IWorkerInstanceIdProvider, WorkerInstanceIdProvider>();
        services.AddSingleton<WorkerHealthState>();
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.OutboxDispatcher));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.OutboxRetry));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.DeadLetterMonitor));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.OutboxCleanup));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.OperationRetry));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.RecoveryResume));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.GrantExpiry));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.ExpiredOperation));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.RetentionCleanup));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.PolicyCacheRefresh));
        services.AddSingleton<IWorkerJob>(sp => CreateJob(sp, sp.GetRequiredService<IOptions<SecurityWorkerOptions>>().Value.ProjectionRefresh));
        services.AddHostedService<WorkerJobHostedService>();

        return services;
    }

    private static IWorkerJob CreateJob(IServiceProvider sp, WorkerJobOptions options) =>
        ActivatorUtilities.CreateInstance<ApplicationBackedWorkerJob>(sp, options);
}
