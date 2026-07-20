namespace XanhNow.Security.Domain.ValueObjects;

public sealed record ExternalReference : StringValueObject
{
    private ExternalReference(string value) : base(value, nameof(ExternalReference), 160)
    {
    }

    public static ExternalReference From(string value) => new(value);
}
