using Microsoft.Extensions.Options;
using XanhNow.Security.Worker.Options;

namespace XanhNow.Security.Worker.Scheduling;

public sealed class WorkerJobHostedService : BackgroundService
{
    private readonly IReadOnlyList<IWorkerJob> _jobs;
    private readonly SecurityWorkerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WorkerJobHostedService> _logger;

    public WorkerJobHostedService(
        IEnumerable<IWorkerJob> jobs,
        IOptions<SecurityWorkerOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<WorkerJobHostedService> logger)
    {
        _jobs = jobs.ToArray();
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabledJobs = _jobs.Where(IsEnabled).ToArray();
        if (enabledJobs.Length == 0)
        {
            _logger.LogWarning("XanhNow.Security Worker started with no enabled jobs.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

        var runners = enabledJobs
            .Select(job => new WorkerJobRunner(job, ResolveInterval(job.Name), _loggerFactory.CreateLogger<WorkerJobRunner>()))
            .Select(runner => runner.StartAsync(stoppingToken))
            .ToArray();

        await Task.WhenAll(runners);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private bool IsEnabled(IWorkerJob job) =>
        _options.AllJobs().First(x => string.Equals(x.Name, job.Name, StringComparison.Ordinal)).Enabled;

    private TimeSpan ResolveInterval(string jobName) =>
        _options.AllJobs().First(job => string.Equals(job.Name, jobName, StringComparison.Ordinal)).Interval;
}
