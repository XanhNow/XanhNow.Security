using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Operations;

public sealed class SecurityOperationRequestTests
{
    [Fact]
    public void Operation_CompletesOnlyAfterRequiredStepsCompleted()
    {
        var at = DateTimeOffset.Parse("2026-07-18T01:07:00Z");
        var stepCode = OperationTypeCode.From("call-passkey-app");
        var operation = SecurityOperationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OperationTypeCode.From("passkey-login"),
            IdempotencyKey.From("idem-passkey-login-001"),
            at,
            at.AddMinutes(10),
            [OperationStepState.Create(Guid.NewGuid(), stepCode, required: true, at)]);

        operation.BeginValidation(at.AddSeconds(1));
        operation.Start(at.AddSeconds(2));
        operation.MarkStepRunning(stepCode, at.AddSeconds(3));
        operation.CompleteStep(stepCode, at.AddSeconds(4));
        operation.Complete(at.AddSeconds(5));

        Assert.Equal(SecurityOperationStatus.Completed, operation.Status);
        Assert.All(operation.Steps, step => Assert.Equal(OperationStepStatus.Completed, step.Status));
    }

    [Fact]
    public void Operation_RejectsCompletionWhenRequiredStepMissing()
    {
        var at = DateTimeOffset.Parse("2026-07-18T01:07:00Z");
        var operation = SecurityOperationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OperationTypeCode.From("login"),
            IdempotencyKey.From("idem-login-001"),
            at,
            at.AddMinutes(10),
            [OperationStepState.Create(Guid.NewGuid(), OperationTypeCode.From("call-login-app"), required: true, at)]);

        operation.BeginValidation(at.AddSeconds(1));
        operation.Start(at.AddSeconds(2));

        var ex = Assert.Throws<DomainException>(() => operation.Complete(at.AddSeconds(3)));

        Assert.Equal("operation_required_steps_incomplete", ex.Code);
    }
}
