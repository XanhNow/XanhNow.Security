namespace XanhNow.Security.Infrastructure.Persistence;

public static class SecurityDatabaseConstants
{
    public const string Schema = "security";
    public const string MigrationHistoryTable = "__ef_migrations_history";

    public const string UsersTable = "security_users";
    public const string ProfilesTable = "security_profiles";
    public const string SessionsTable = "security_sessions";
    public const string GrantsTable = "security_grants";
    public const string PoliciesTable = "security_policies";
    public const string PolicyDecisionsTable = "security_policy_decisions";
    public const string RecoveryCasesTable = "security_recovery_cases";
    public const string OperationRequestsTable = "security_operation_requests";
    public const string AuditLogsTable = "security_audit_logs";
    public const string OutboxMessagesTable = "security_outbox_messages";

    public static readonly string[] ExpectedTables =
    [
        UsersTable,
        ProfilesTable,
        SessionsTable,
        GrantsTable,
        PoliciesTable,
        PolicyDecisionsTable,
        RecoveryCasesTable,
        OperationRequestsTable,
        AuditLogsTable,
        OutboxMessagesTable
    ];
}
