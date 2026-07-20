namespace XanhNow.Security.Infrastructure.Persistence;

public sealed class SecurityPersistenceOptions
{
    public string? ConnectionString { get; init; }
    public bool EnableSensitiveDataLogging { get; init; }
    public bool EnableDetailedErrors { get; init; }
}
