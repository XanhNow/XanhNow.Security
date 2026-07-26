using XanhNow.Security.Application.Abstractions.Time;

namespace XanhNow.Security.Worker.Runtime;

public sealed class WorkerSystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
