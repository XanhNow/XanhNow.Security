namespace XanhNow.Security.Domain.ValueObjects;

public sealed record AuditAction : StringValueObject
{
    private AuditAction(string value) : base(value, nameof(AuditAction), 128)
    {
    }

    public static AuditAction From(string value) => new(value);
}
