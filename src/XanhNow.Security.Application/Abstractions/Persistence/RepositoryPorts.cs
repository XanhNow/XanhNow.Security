using XanhNow.Security.Domain.Audit;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Domain.Profiles;
using XanhNow.Security.Domain.Recovery;
using XanhNow.Security.Domain.Sessions;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Abstractions.Persistence;

public interface ISecurityUserRepository
{
    ValueTask<SecurityUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    ValueTask AddAsync(SecurityUser user, CancellationToken cancellationToken);
}

public interface ISecurityGrantRepository
{
    ValueTask<SecurityGrant?> FindByIdAsync(Guid grantId, CancellationToken cancellationToken);
    ValueTask AddAsync(SecurityGrant grant, CancellationToken cancellationToken);
}

public interface ISecurityPolicyRepository
{
    ValueTask<SecurityPolicy?> FindActiveByCodeAsync(PolicyCode code, CancellationToken cancellationToken);
    ValueTask AddAsync(SecurityPolicy policy, CancellationToken cancellationToken);
}

public interface ISecurityRecoveryCaseRepository
{
    ValueTask<SecurityRecoveryCase?> FindByIdAsync(Guid recoveryCaseId, CancellationToken cancellationToken);
    ValueTask AddAsync(SecurityRecoveryCase recoveryCase, CancellationToken cancellationToken);
}

public interface ISecurityOperationRepository
{
    ValueTask<SecurityOperationRequest?> FindByIdAsync(Guid operationId, CancellationToken cancellationToken);
    ValueTask<SecurityOperationRequest?> FindByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken);
    ValueTask AddAsync(SecurityOperationRequest operation, CancellationToken cancellationToken);
}

public interface ISecurityProfileReader
{
    ValueTask<SecurityProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ISecuritySessionReader
{
    ValueTask<PageResult<SecuritySession>> ListActiveByUserIdAsync(Guid userId, PageRequest page, CancellationToken cancellationToken);
}

public interface ISecurityPolicyDecisionWriter
{
    ValueTask AppendAsync(SecurityPolicyDecision decision, CancellationToken cancellationToken);
}

public interface ISecurityAuditLogWriter
{
    ValueTask AppendAsync(SecurityAuditLog auditLog, CancellationToken cancellationToken);
}

public interface ILocalUnitOfWork
{
    ValueTask CommitAsync(CancellationToken cancellationToken);
}
