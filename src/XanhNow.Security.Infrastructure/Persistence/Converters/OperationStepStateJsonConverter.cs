using XanhNow.Security.Domain.Operations;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Infrastructure.Persistence.Converters;

internal sealed record OperationStepStateJson(Guid Id, string StepCode, bool Required, OperationStepStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, string? FailureCode)
{
    public static OperationStepStateJson FromDomain(OperationStepState step)
        => new(step.Id, step.StepCode.Value, step.Required, step.Status, step.CreatedAt, step.StartedAt, step.CompletedAt, step.FailureCode);

    public OperationStepState ToDomain()
        => OperationStepState.Restore(Id, OperationTypeCode.From(StepCode), Required, Status, CreatedAt, StartedAt, CompletedAt, FailureCode);
}

internal static class OperationStepStateJsonConverter
{
    public static string ToJson(IEnumerable<OperationStepState> steps)
        => PersistenceJson.Serialize(steps.Select(OperationStepStateJson.FromDomain).ToArray());

    public static List<OperationStepState> FromJson(string json)
        => PersistenceJson.Deserialize<OperationStepStateJson[]>(json).Select(x => x.ToDomain()).ToList();
}
