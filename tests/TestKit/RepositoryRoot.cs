namespace XanhNow.Security.Tests.TestKit;

public static class RepositoryRoot
{
    public static string Find()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "XanhNow.Security.slnx")) &&
                Directory.Exists(Path.Combine(current, "src")) &&
                Directory.Exists(Path.Combine(current, "tests")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Cannot find XanhNow.Security repository root.");
    }
}
