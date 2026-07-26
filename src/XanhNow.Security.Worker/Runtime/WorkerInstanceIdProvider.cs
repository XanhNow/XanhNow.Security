using XanhNow.Security.Worker.Options;

namespace XanhNow.Security.Worker.Runtime;

public sealed class WorkerInstanceIdProvider : IWorkerInstanceIdProvider
{
    public WorkerInstanceIdProvider(IHostEnvironment environment)
    {
        var host = Environment.MachineName.ToLowerInvariant();
        InstanceId = $"{SecurityWorkerOptions.SectionName}:{environment.ApplicationName}:{host}:{Guid.NewGuid():N}";
    }

    public string InstanceId { get; }
}
