namespace XanhNow.Security.Domain.ValueObjects;

public sealed record GrantPurpose : StringValueObject
{
    private GrantPurpose(string value) : base(value, nameof(GrantPurpose), 128)
    {
    }

    public static GrantPurpose From(string value) => new(value);
}
