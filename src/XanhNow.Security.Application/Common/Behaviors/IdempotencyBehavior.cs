using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IIdempotencyStore _store;
    private readonly IRequestFingerprint _fingerprint;

    public IdempotencyBehavior(IIdempotencyStore store, IRequestFingerprint fingerprint)
    {
        _store = store;
        _fingerprint = fingerprint;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotent)
        {
            return await next(cancellationToken);
        }

        var requestHash = _fingerprint.Compute(request);
        var existing = await _store.FindAsync(idempotent.IdempotencyKey, cancellationToken);
        if (existing is not null && existing.RequestHash != requestHash)
        {
            return Result<TResponse>.Failure(Error.Conflict(SecurityErrorCodes.IdempotencyConflict, "Idempotency key is already used by another request."));
        }

        if (existing is null)
        {
            await _store.ReserveAsync(idempotent.IdempotencyKey, requestHash, cancellationToken);
        }

        return await next(cancellationToken);
    }
}
