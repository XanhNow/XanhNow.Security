namespace XanhNow.Security.Domain.ValueObjects;

public sealed record OperationTypeCode : StringValueObject
{
    private OperationTypeCode(string value) : base(value, nameof(OperationTypeCode), 128)
    {
    }

    public static OperationTypeCode From(string value) => new(value);
}
