namespace XanhNow.Security.Migrator;

public enum MigratorExitCode
{
    Success = 0,
    InvalidArguments = 2,
    ConfigurationError = 10,
    CredentialUnavailable = 20,
    PreflightFailed = 30,
    LockUnavailable = 40,
    MigrationFailed = 50,
    VerificationFailed = 60,
    Cancelled = 70,
    UnexpectedFailure = 99
}
