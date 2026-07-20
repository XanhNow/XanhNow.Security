namespace XanhNow.Security.Application.Abstractions.Context;

public sealed record CallerContext(
    CallerType Type,
    Guid? UserId,
    string Subject,
    IReadOnlySet<string> Permissions)
{
    public bool IsAuthenticated => Type != CallerType.Anonymous && !string.IsNullOrWhiteSpace(Subject);

    public static CallerContext Anonymous { get; } = new(CallerType.Anonymous, null, "anonymous", new HashSet<string>());
}
