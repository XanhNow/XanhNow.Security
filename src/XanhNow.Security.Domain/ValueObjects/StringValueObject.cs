using XanhNow.Security.Domain.Common;

namespace XanhNow.Security.Domain.ValueObjects;

public abstract record StringValueObject
{
    protected StringValueObject(string value, string name, int maxLength = 128)
    {
        Value = Guard.NotBlank(value, name, maxLength);
    }

    public string Value { get; }

    public override string ToString() => Value;
}
