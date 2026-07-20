using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Operations;

public sealed class SecurityOperationManager : ISecurityOperationManager
{
    private readonly ISecurityOperationRepository _operations;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public SecurityOperationManager(ISecurityOperationRepository operations, ILocalUnitOfWork unitOfWork, IClock clock, IIdGenerator idGenerator)
    {
        _operations = operations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async ValueTask<SecurityOperationRequest> CreateOrGetAsync(CreateOperationRequest request, CancellationToken cancellationToken)
    {
        var existing = await _operations.FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId != request.UserId || existing.OperationType != request.OperationType)
            {
                throw new DomainException(SecurityErrorCodes.IdempotencyConflict, "Operation idempotency key conflicts with another request.");
            }

            return existing;
        }

        var now = _clock.UtcNow;
        var steps = request.Steps.Select(step => OperationStepState.Create(_idGenerator.NewId(), step.StepCode, step.Required, now)).ToArray();
        var operation = SecurityOperationRequest.Create(_idGenerator.NewId(), request.UserId, request.OperationType, request.IdempotencyKey, now, now.Add(request.TimeToLive), steps);
        operation.BeginValidation(now);
        operation.Start(now);

        await _operations.AddAsync(operation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return operation;
    }

    public async ValueTask MarkStepRunningAsync(Guid operationId, OperationTypeCode stepCode, CancellationToken cancellationToken)
    {
        var operation = await LoadAsync(operationId, cancellationToken);
        operation.MarkStepRunning(stepCode, _clock.UtcNow);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async ValueTask CompleteStepAsync(Guid operationId, OperationTypeCode stepCode, CancellationToken cancellationToken)
    {
        var operation = await LoadAsync(operationId, cancellationToken);
        operation.CompleteStep(stepCode, _clock.UtcNow);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async ValueTask CompleteAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var operation = await LoadAsync(operationId, cancellationToken);
        operation.Complete(_clock.UtcNow);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async ValueTask FailSafeAsync(Guid operationId, string failureCode, CancellationToken cancellationToken)
    {
        var operation = await LoadAsync(operationId, cancellationToken);
        operation.FailSafe(failureCode, _clock.UtcNow);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private async ValueTask<SecurityOperationRequest> LoadAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var operation = await _operations.FindByIdAsync(operationId, cancellationToken);
        return operation ?? throw new DomainException(SecurityErrorCodes.OperationNotFound, "Security operation was not found.");
    }
}
