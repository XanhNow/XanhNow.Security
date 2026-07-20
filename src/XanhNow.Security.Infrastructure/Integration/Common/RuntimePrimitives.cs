using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Time;

namespace XanhNow.Security.Infrastructure.Integration.Common;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

internal sealed class GuidIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
