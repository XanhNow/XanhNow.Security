using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Domain.Policies;

namespace XanhNow.Security.Infrastructure.Persistence.Writers;

internal sealed class SecurityPolicyDecisionWriter : ISecurityPolicyDecisionWriter
{
    private readonly SecurityDbContext _db;

    public SecurityPolicyDecisionWriter(SecurityDbContext db) => _db = db;

    public async ValueTask AppendAsync(SecurityPolicyDecision decision, CancellationToken cancellationToken)
        => await _db.SecurityPolicyDecisions.AddAsync(decision, cancellationToken);
}
