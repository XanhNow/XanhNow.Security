namespace XanhNow.Security.Api.Options;

public sealed class SecurityApiOptions
{
    public const string SectionName = "SecurityApi";
    public string ServiceName { get; set; } = "XanhNow.Security";
    public string ContractVersion { get; set; } = "v1";
    public bool RequireHttps { get; set; }
    public string[] AllowedOrigins { get; set; } = [];
    public string[] KnownProxies { get; set; } = [];
    public int MaxRequestBodyBytes { get; set; } = 1_048_576;
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int AnonymousRequestsPerMinute { get; set; } = 60;
    public int UserRequestsPerMinute { get; set; } = 120;
    public int ServiceRequestsPerMinute { get; set; } = 300;
}
