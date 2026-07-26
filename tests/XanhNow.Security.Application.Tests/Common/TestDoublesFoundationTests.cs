using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Tests.TestKit;

namespace XanhNow.Security.Application.Tests.Common;

public sealed class TestDoublesFoundationTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    public void Deterministic_guid_sequence_is_repeatable_for_application_tests()
    {
        var generator = new TestIdGenerator(new DeterministicGuidSequence());

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), generator.NewId());
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), generator.NewId());
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    public void Manual_clock_can_move_time_forward_without_system_clock()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(DateTimeOffset.Parse("2026-07-18T01:12:00Z"), clock.UtcNow);
    }

    private sealed class TestIdGenerator : IIdGenerator
    {
        private readonly DeterministicGuidSequence _sequence;

        public TestIdGenerator(DeterministicGuidSequence sequence) => _sequence = sequence;

        public Guid NewId() => _sequence.Next();
    }

    private sealed class TestClock : IClock
    {
        private readonly ManualTestClock _clock;

        public TestClock(DateTimeOffset utcNow) => _clock = new ManualTestClock(utcNow);

        public DateTimeOffset UtcNow => _clock.UtcNow;

        public void Advance(TimeSpan value) => _clock.Advance(value);
    }
}
