namespace XanhNow.Security.Migrator.Planning;

public sealed record MigrationPlan(IReadOnlyList<string> AppliedMigrations, IReadOnlyList<string> PendingMigrations)
{
    public bool HasPendingMigrations => PendingMigrations.Count > 0;
}
