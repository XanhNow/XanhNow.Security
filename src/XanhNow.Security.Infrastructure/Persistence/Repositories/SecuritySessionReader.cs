using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Sessions;

namespace XanhNow.Security.Infrastructure.Persistence.Repositories;

internal sealed class SecuritySessionReader : ISecuritySessionReader
{
    private readonly SecurityDbContext _db;

    public SecuritySessionReader(SecurityDbContext db) => _db = db;

    public async ValueTask<PageResult<SecuritySession>> ListActiveByUserIdAsync(Guid userId, PageRequest page, CancellationToken cancellationToken)
    {
        var query = _db.SecuritySessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.IssuedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new PageResult<SecuritySession>(items, total, page.PageNumber, page.PageSize);
    }
}
