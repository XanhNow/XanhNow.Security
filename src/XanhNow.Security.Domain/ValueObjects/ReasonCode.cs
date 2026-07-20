namespace XanhNow.Security.Domain.ValueObjects;

public sealed record ReasonCode : StringValueObject
{
    private ReasonCode(string value) : base(value, nameof(ReasonCode), 128)
    {
    }

    public static ReasonCode From(string value) => new(value);
}
