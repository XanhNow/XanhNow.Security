namespace XanhNow.Security.Tests.TestKit;

public sealed class ManualTestClock
{
    public ManualTestClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
}
