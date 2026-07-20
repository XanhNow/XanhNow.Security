using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityPolicyRepository : ISecurityPolicyRepository
{
    private readonly SecurityDbContext _db;

    public SecurityPolicyRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityPolicy?> FindActiveByCodeAsync(PolicyCode code, CancellationToken cancellationToken)
        => await _db.SecurityPolicies.SingleOrDefaultAsync(x => x.Code == code && x.Status == SecurityPolicyStatus.Active, cancellationToken);

    public async ValueTask AddAsync(SecurityPolicy policy, CancellationToken cancellationToken)
        => await _db.SecurityPolicies.AddAsync(policy, cancellationToken);
}
