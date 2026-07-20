using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Profiles;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityProfileReader : ISecurityProfileReader
{
    private readonly SecurityDbContext _db;

    public SecurityProfileReader(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.SecurityProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
}
