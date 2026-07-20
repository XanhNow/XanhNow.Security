namespace XanhNow.Security.Infrastructure.Integration.Options;

public sealed class SecurityIntegrationOptionsValidator
{
    public IReadOnlyCollection<string> Validate(SecurityIntegrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var errors = new List<string>();
        ValidateChild(options.AuthLogin, errors);
        ValidateChild(options.Jwt, errors);
        ValidateChild(options.Passkey, errors);
        ValidateChild(options.SmartOtp, errors);

        if (options.DefaultDeadline <= TimeSpan.Zero)
        {
            errors.Add("DefaultDeadline must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.ContractVersion))
        {
            errors.Add("ContractVersion is required.");
        }

        if (!string.IsNullOrWhiteSpace(options.Redis.KeyPrefix) && options.Redis.KeyPrefix.Any(char.IsWhiteSpace))
        {
            errors.Add("Redis KeyPrefix must not contain whitespace.");
        }

        if (options.Redis.DefaultCacheTtl <= TimeSpan.Zero)
        {
            errors.Add("Redis DefaultCacheTtl must be positive.");
        }

        if (options.Redis.IdempotencyTtl <= TimeSpan.Zero)
        {
            errors.Add("Redis IdempotencyTtl must be positive.");
        }

        if (options.Redis.LockTtl <= TimeSpan.Zero)
        {
            errors.Add("Redis LockTtl must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.Kafka.SecurityEventsTopic))
        {
            errors.Add("Kafka SecurityEventsTopic is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Kafka.SecurityAuditTopic))
        {
            errors.Add("Kafka SecurityAuditTopic is required.");
        }

        return errors;
    }

    public void ValidateAndThrow(SecurityIntegrationOptions options)
    {
        var errors = Validate(options);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid XanhNow.Security integration options: " + string.Join("; ", errors));
        }
    }

    private static void ValidateChild(ChildAppClientOptions child, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(child.Name))
        {
            errors.Add("Child app Name is required.");
        }

        if (!Uri.TryCreate(child.BaseAddress, UriKind.Absolute, out _))
        {
            errors.Add($"Child app {child.Name} BaseAddress must be absolute.");
        }

        if (child.Deadline <= TimeSpan.Zero)
        {
            errors.Add($"Child app {child.Name} Deadline must be positive.");
        }

        if (child.RequiresMtls && string.IsNullOrWhiteSpace(child.TrustedCaPath))
        {
            errors.Add($"Child app {child.Name} requires TrustedCaPath when mTLS is enabled.");
        }
    }
}
