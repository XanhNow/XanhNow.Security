namespace XanhNow.Security.Domain.Common;

public static class Guard
{
    public static string NotBlank(string value, string name, int maxLength = 256)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("value_required", $"{name} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException("value_too_long", $"{name} is too long.");
        }

        return normalized;
    }

    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("id_required", $"{name} is required.");
        }

        return value;
    }

    public static void True(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}
