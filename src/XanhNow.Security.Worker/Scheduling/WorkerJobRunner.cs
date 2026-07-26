namespace XanhNow.Security.Worker.Scheduling;

public sealed class WorkerJobRunner : BackgroundService
{
    private readonly IWorkerJob _job;
    private readonly TimeSpan _interval;
    private readonly ILogger<WorkerJobRunner> _logger;

    public WorkerJobRunner(IWorkerJob job, TimeSpan interval, ILogger<WorkerJobRunner> logger)
    {
        _job = job;
        _interval = interval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting worker job {JobName} with interval {Interval}.", _job.Name, _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _job.RunAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
