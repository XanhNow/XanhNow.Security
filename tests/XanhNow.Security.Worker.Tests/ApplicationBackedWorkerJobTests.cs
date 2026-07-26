using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Background;
using XanhNow.Security.Application.Background.Commands;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Worker.Jobs;
using XanhNow.Security.Worker.Options;
using XanhNow.Security.Worker.Runtime;

namespace XanhNow.Security.Worker.Tests;

public sealed class ApplicationBackedWorkerJobTests
{
    [Fact]
    public async Task RunAsync_creates_application_scope_and_records_success()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<IClock>(new FixedClock(new DateTimeOffset(2026, 7, 20, 1, 0, 0, TimeSpan.Zero)));
        services.AddScoped<IRequestHandler<RunBackgroundJobCommand, BackgroundCommandResult>, RunBackgroundJobCommandHandler>();
        services.AddScoped<ApplicationExecutor<RunBackgroundJobCommand, BackgroundCommandResult>>();
        var provider = services.BuildServiceProvider();
        var health = new WorkerHealthState();
        var job = new ApplicationBackedWorkerJob(
            new WorkerJobOptions { Name = WorkerJobNames.OutboxDispatcher, Enabled = true },
            provider.GetRequiredService<IServiceScopeFactory>(),
            new FixedWorkerInstanceIdProvider("worker-test-1"),
            health,
            provider.GetRequiredService<ILogger<ApplicationBackedWorkerJob>>());

        await job.RunAsync(CancellationToken.None);

        var snapshot = Assert.Single(health.Snapshot());
        Assert.Equal(WorkerJobNames.OutboxDispatcher, snapshot.JobName);
        Assert.Null(snapshot.LastErrorCode);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FixedWorkerInstanceIdProvider : IWorkerInstanceIdProvider
    {
        public FixedWorkerInstanceIdProvider(string instanceId) => InstanceId = instanceId;
        public string InstanceId { get; }
    }
}
