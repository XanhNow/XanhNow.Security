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
    public int MaxRequestHeadersTotalSizeBytes { get; set; } = 32_768;
    public int RequestHeadersTimeoutSeconds { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int MaxCorrelationIdLength { get; set; } = 128;
    public bool EnableSecurityHeaders { get; set; } = true;
    public bool EnableStrictTransportSecurity { get; set; }
    public int AnonymousRequestsPerMinute { get; set; } = 60;
    public int UserRequestsPerMinute { get; set; } = 120;
    public int ServiceRequestsPerMinute { get; set; } = 300;
    public Dictionary<string, string> InternalServiceApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
