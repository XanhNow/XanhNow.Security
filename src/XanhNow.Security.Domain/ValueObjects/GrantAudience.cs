namespace XanhNow.Security.Domain.ValueObjects;

public sealed record GrantAudience : StringValueObject
{
    private GrantAudience(string value) : base(value, nameof(GrantAudience), 128)
    {
    }

    public static GrantAudience From(string value) => new(value);
}
