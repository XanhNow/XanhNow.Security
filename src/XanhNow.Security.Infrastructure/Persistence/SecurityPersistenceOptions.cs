namespace XanhNow.Security.Infrastructure.Persistence;

public sealed class SecurityPersistenceOptions
{
    public string? ConnectionString { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableDetailedErrors { get; set; }
}

