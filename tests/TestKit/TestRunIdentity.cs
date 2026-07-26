namespace XanhNow.Security.Tests.TestKit;

public sealed record TestRunIdentity(string RunId, string RepositoryRoot, DateTimeOffset StartedAtUtc)
{
    public static TestRunIdentity Create(string repositoryRoot)
    {
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        if (string.IsNullOrWhiteSpace(runId))
        {
            runId = $"local-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        }

        return new TestRunIdentity(runId, repositoryRoot, DateTimeOffset.UtcNow);
    }
}
