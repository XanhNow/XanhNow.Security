namespace XanhNow.Security.Migrator.Credentials;

public interface IMigratorCredentialProvider
{
    Task<string> LoadConnectionStringAsync(CancellationToken cancellationToken);
}

public sealed class MigratorCredentialException : Exception
{
    public MigratorCredentialException(string message) : base(message)
    {
    }
}
