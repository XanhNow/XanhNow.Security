namespace XanhNow.Security.Domain.ValueObjects;

public sealed record PolicyCode : StringValueObject
{
    private PolicyCode(string value) : base(value, nameof(PolicyCode), 128)
    {
    }

    public static PolicyCode From(string value) => new(value);
}
