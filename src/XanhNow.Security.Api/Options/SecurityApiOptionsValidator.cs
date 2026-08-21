using Microsoft.Extensions.Options;

namespace XanhNow.Security.Api.Options;

public sealed class SecurityApiOptionsValidator : IValidateOptions<SecurityApiOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityApiOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            failures.Add("SecurityApi:ServiceName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ContractVersion))
        {
            failures.Add("SecurityApi:ContractVersion is required.");
        }

        if (options.MaxRequestBodyBytes <= 0)
        {
            failures.Add("SecurityApi:MaxRequestBodyBytes must be greater than zero.");
        }

        if (options.MaxRequestHeadersTotalSizeBytes < 8_192)
        {
            failures.Add("SecurityApi:MaxRequestHeadersTotalSizeBytes must be at least 8192 bytes.");
        }

        if (options.RequestHeadersTimeoutSeconds <= 0)
        {
            failures.Add("SecurityApi:RequestHeadersTimeoutSeconds must be greater than zero.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("SecurityApi:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.MaxCorrelationIdLength is < 32 or > 256)
        {
            failures.Add("SecurityApi:MaxCorrelationIdLength must be between 32 and 256.");
        }

        foreach (var item in options.InternalServiceApiKeys)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
            {
                failures.Add("SecurityApi:InternalServiceApiKeys cannot contain empty service names or keys.");
            }
        }

        foreach (var item in options.InternalServiceApiKeyFiles)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
            {
                failures.Add("SecurityApi:InternalServiceApiKeyFiles cannot contain empty service names or paths.");
            }
        }

        if (options.AnonymousRequestsPerMinute <= 0 || options.UserRequestsPerMinute <= 0 || options.ServiceRequestsPerMinute <= 0)
        {
            failures.Add("SecurityApi rate limits must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
