namespace XanhNow.Security.Migrator.Verification;

public sealed record TargetPreflightResult(string DatabaseName, string RoleName, string CurrentSchema, bool IsExpected)
{
    public string Detail => $"database={DatabaseName}; role={RoleName}; schema={CurrentSchema}";
}
