using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Operations;

public interface ISecurityOperationManager
{
    ValueTask<SecurityOperationRequest> CreateOrGetAsync(CreateOperationRequest request, CancellationToken cancellationToken);
    ValueTask MarkStepRunningAsync(Guid operationId, OperationTypeCode stepCode, CancellationToken cancellationToken);
    ValueTask CompleteStepAsync(Guid operationId, OperationTypeCode stepCode, CancellationToken cancellationToken);
    ValueTask CompleteAsync(Guid operationId, CancellationToken cancellationToken);
    ValueTask FailSafeAsync(Guid operationId, string failureCode, CancellationToken cancellationToken);
}
