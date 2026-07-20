using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Users;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecurityUserRepository : ISecurityUserRepository
{
    private readonly SecurityDbContext _db;

    public SecurityUserRepository(SecurityDbContext db) => _db = db;

    public async ValueTask<SecurityUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.SecurityUsers.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async ValueTask AddAsync(SecurityUser user, CancellationToken cancellationToken)
        => await _db.SecurityUsers.AddAsync(user, cancellationToken);
}
