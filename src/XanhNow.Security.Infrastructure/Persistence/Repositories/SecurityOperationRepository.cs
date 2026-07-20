using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityOperationRepository : ISecurityOperationRepository
{
    private readonly SecurityDbContext _db;

    public SecurityOperationRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityOperationRequest?> FindByIdAsync(Guid operationId, CancellationToken cancellationToken)
        => await _db.SecurityOperationRequests.SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

    public async ValueTask<SecurityOperationRequest?> FindByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken)
        => await _db.SecurityOperationRequests.SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async ValueTask AddAsync(SecurityOperationRequest operation, CancellationToken cancellationToken)
        => await _db.SecurityOperationRequests.AddAsync(operation, cancellationToken);
}
