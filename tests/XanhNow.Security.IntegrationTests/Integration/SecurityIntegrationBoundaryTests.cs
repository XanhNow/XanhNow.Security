namespace XanhNow.Security.IntegrationTests.Integration;

public sealed class SecurityIntegrationBoundaryTests
{
    [Fact]
    public void IntegrationSource_DoesNotContainHardCodedSecrets()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "XanhNow.Security.Infrastructure", "Integration"), "*.cs", SearchOption.AllDirectories);
        var forbidden = new[]
        {
            "Password=",
            "secret_id=",
            "BEGIN PRIVATE KEY",
            "RedisConfiguration=",
            "XanhNowAuth@",
            "JwtMigratorTest2026"
        };

        var hits = files
            .SelectMany(file => File.ReadAllLines(file).Select((line, index) => new { file, line, index }))
            .Where(row => forbidden.Any(token => row.line.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Select(row => $"{row.file}:{row.index + 1}:{row.line}")
            .ToArray();

        Assert.Empty(hits);
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (Directory.Exists(Path.Combine(current, "src")) && Directory.Exists(Path.Combine(current, "tests")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Cannot find repository root.");
    }
}
