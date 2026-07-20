namespace XanhNow.Security.Infrastructure.Integration.Common;

internal static class Redaction
{
    private static readonly string[] SensitiveTokens = ["password", "secret", "token", "totp", "private", "credential"];

    public static string Safe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return SensitiveTokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase)) ? "[REDACTED]" : value;
    }
}
