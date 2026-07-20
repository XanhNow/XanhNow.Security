using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Infrastructure.Persistence.Transactions;

internal sealed class LocalUnitOfWork : ILocalUnitOfWork
{
    private readonly SecurityDbContext _db;

    public LocalUnitOfWork(SecurityDbContext db) => _db = db;

    public async ValueTask CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Security state was changed by another writer.");
        }
    }
}

