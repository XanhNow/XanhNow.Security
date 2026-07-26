namespace XanhNow.Security.Worker.Options;

public sealed class WorkerJobOptions
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public int BatchSize { get; set; } = 25;
    public int MaxConcurrency { get; set; } = 1;
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(2);
    public int MaxAttempts { get; set; } = 5;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public int JitterPercent { get; set; } = 20;
}
