using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Recovery;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityRecoveryCaseRepository : ISecurityRecoveryCaseRepository
{
    private readonly SecurityDbContext _db;

    public SecurityRecoveryCaseRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityRecoveryCase?> FindByIdAsync(Guid recoveryCaseId, CancellationToken cancellationToken)
        => await _db.SecurityRecoveryCases.SingleOrDefaultAsync(x => x.Id == recoveryCaseId, cancellationToken);

    public async ValueTask AddAsync(SecurityRecoveryCase recoveryCase, CancellationToken cancellationToken)
        => await _db.SecurityRecoveryCases.AddAsync(recoveryCase, cancellationToken);
}
