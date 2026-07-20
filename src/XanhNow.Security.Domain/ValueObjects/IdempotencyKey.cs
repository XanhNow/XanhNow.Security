namespace XanhNow.Security.Domain.ValueObjects;

public sealed record IdempotencyKey : StringValueObject
{
    private IdempotencyKey(string value) : base(value, nameof(IdempotencyKey), 160)
    {
    }

    public static IdempotencyKey From(string value) => new(value);
}
