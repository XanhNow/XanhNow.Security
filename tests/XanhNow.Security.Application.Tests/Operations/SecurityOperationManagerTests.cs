using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Operations;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Tests.Operations;

public sealed class SecurityOperationManagerTests
{
    [Fact]
    public async Task CreateOrGetAsync_CreatesRunningOperationAndCommits()
    {
        var repo = new InMemoryOperationRepository();
        var uow = new CountingUnitOfWork();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-18T01:07:00Z"));
        var manager = new SecurityOperationManager(repo, uow, clock, new SequentialIdGenerator());
        var step = OperationTypeCode.From("call-login-app");

        var operation = await manager.CreateOrGetAsync(new CreateOperationRequest(
            Guid.NewGuid(),
            OperationTypeCode.From("login"),
            IdempotencyKey.From("idem-login-001"),
            TimeSpan.FromMinutes(10),
            [new OperationStepPlan(step, Required: true)]), CancellationToken.None);

        Assert.Equal(SecurityOperationStatus.Running, operation.Status);
        Assert.Single(operation.Steps);
        Assert.Equal(1, uow.CommitCount);
    }

    [Fact]
    public async Task CreateOrGetAsync_ReturnsExistingOperationForSameIdempotencyKey()
    {
        var repo = new InMemoryOperationRepository();
        var uow = new CountingUnitOfWork();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-18T01:07:00Z"));
        var manager = new SecurityOperationManager(repo, uow, clock, new SequentialIdGenerator());
        var userId = Guid.NewGuid();
        var request = new CreateOperationRequest(userId, OperationTypeCode.From("login"), IdempotencyKey.From("idem-login-001"), TimeSpan.FromMinutes(10), [new OperationStepPlan(OperationTypeCode.From("call-login-app"), true)]);

        var first = await manager.CreateOrGetAsync(request, CancellationToken.None);
        var second = await manager.CreateOrGetAsync(request, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, repo.AddCount);
    }

    private sealed class InMemoryOperationRepository : ISecurityOperationRepository
    {
        private readonly List<SecurityOperationRequest> _items = [];
        public int AddCount { get; private set; }

        public ValueTask<SecurityOperationRequest?> FindByIdAsync(Guid operationId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_items.SingleOrDefault(item => item.Id == operationId));

        public ValueTask<SecurityOperationRequest?> FindByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken)
            => ValueTask.FromResult(_items.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey));

        public ValueTask AddAsync(SecurityOperationRequest operation, CancellationToken cancellationToken)
        {
            AddCount++;
            _items.Add(operation);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingUnitOfWork : ILocalUnitOfWork
    {
        public int CommitCount { get; private set; }
        public ValueTask CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class SequentialIdGenerator : IIdGenerator
    {
        private int _value;
        public Guid NewId()
        {
            _value++;
            return Guid.Parse($"00000000-0000-0000-0000-{_value:000000000000}");
        }
    }
}
