namespace XanhNow.Security.Tests.TestKit;

public sealed class DeterministicGuidSequence
{
    private long _value;

    public Guid Next()
    {
        var value = Interlocked.Increment(ref _value);
        return Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
    }
}
