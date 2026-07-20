using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Grants;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityGrantRepository : ISecurityGrantRepository
{
    private readonly SecurityDbContext _db;

    public SecurityGrantRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityGrant?> FindByIdAsync(Guid grantId, CancellationToken cancellationToken)
        => await _db.SecurityGrants.SingleOrDefaultAsync(x => x.Id == grantId, cancellationToken);

    public async ValueTask AddAsync(SecurityGrant grant, CancellationToken cancellationToken)
        => await _db.SecurityGrants.AddAsync(grant, cancellationToken);
}
