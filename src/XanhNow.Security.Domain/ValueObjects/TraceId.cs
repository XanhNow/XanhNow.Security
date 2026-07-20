namespace XanhNow.Security.Domain.ValueObjects;

public sealed record TraceId : StringValueObject
{
    private TraceId(string value) : base(value, nameof(TraceId), 128)
    {
    }

    public static TraceId From(string value) => new(value);
}
