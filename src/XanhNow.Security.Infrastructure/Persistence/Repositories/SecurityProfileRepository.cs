using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Profiles;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityProfileRepository : ISecurityProfileWriter
{
    private readonly SecurityDbContext _db;

    public SecurityProfileRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.SecurityProfiles.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async ValueTask AddAsync(SecurityProfile profile, CancellationToken cancellationToken)
        => await _db.SecurityProfiles.AddAsync(profile, cancellationToken);
}
