namespace XanhNow.Security.Application.Abstractions.Policy;

public sealed record PolicyContext(
    Guid? UserId,
    string Action,
    string AssuranceLevel,
    IReadOnlyDictionary<string, string> Metadata);
