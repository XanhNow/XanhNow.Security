namespace XanhNow.Security.Application.Abstractions.Idempotency;

public interface IIdempotencyStore
{
    ValueTask<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);
    ValueTask ReserveAsync(string key, string requestHash, CancellationToken cancellationToken);
    ValueTask CompleteAsync(string key, string resultJson, CancellationToken cancellationToken);
}
