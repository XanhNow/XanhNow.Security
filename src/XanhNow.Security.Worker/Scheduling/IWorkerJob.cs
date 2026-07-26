namespace XanhNow.Security.Worker.Scheduling;

public interface IWorkerJob
{
    string Name { get; }
    Task RunAsync(CancellationToken cancellationToken);
}
