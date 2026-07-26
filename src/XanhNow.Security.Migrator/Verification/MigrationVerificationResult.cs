namespace XanhNow.Security.Migrator.Verification;

public sealed record MigrationVerificationResult(IReadOnlyList<string> PendingMigrations, IReadOnlyList<string> MissingTables)
{
    public bool IsValid => PendingMigrations.Count == 0 && MissingTables.Count == 0;
}
