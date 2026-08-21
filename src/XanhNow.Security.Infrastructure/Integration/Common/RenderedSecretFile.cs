namespace XanhNow.Security.Infrastructure.Integration.Common;

public static class RenderedSecretFile
{
    public static string? ReadTrimmed(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var value = File.ReadAllText(path).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
