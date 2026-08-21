using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XanhNow.Security.Application.Abstractions.Caching;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Application.Abstractions.Grant;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Locking;
using XanhNow.Security.Application.Abstractions.Policy;
using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Infrastructure.Integration.ChildApps.AuthLogin;
using XanhNow.Security.Infrastructure.Integration.ChildApps.Jwt;
using XanhNow.Security.Infrastructure.Integration.ChildApps.Passkey;
using XanhNow.Security.Infrastructure.Integration.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Kafka;
using XanhNow.Security.Infrastructure.Integration.Options;
using XanhNow.Security.Infrastructure.Integration.Policy;
using XanhNow.Security.Infrastructure.Integration.Redis;
using XanhNow.Security.Infrastructure.Integration.Vault;

namespace XanhNow.Security.Infrastructure.Integration;

public static class SecurityIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityIntegration(this IServiceCollection services, Action<SecurityIntegrationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSingleton<SecurityIntegrationOptionsValidator>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecurityIntegrationOptions>>().Value;
            ApplyRenderedSecretFiles(options);
            sp.GetRequiredService<SecurityIntegrationOptionsValidator>().ValidateAndThrow(options);
            return options;
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddSingleton<IRequestFingerprint, JsonRequestFingerprint>();
        services.AddSingleton<RedisRuntimeState>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<SecurityIntegrationOptions>();
            if (!string.Equals(options.Redis.Mode, "Redis", StringComparison.OrdinalIgnoreCase))
            {
                return new RedisConnectionProvider(null);
            }

            var configuration = BuildRedisConfiguration(options, sp.GetRequiredService<IVaultSecretReader>());
            return new RedisConnectionProvider(ConnectionMultiplexer.Connect(configuration));
        });
        services.AddSingleton<IApplicationCache, RedisApplicationCache>();
        services.AddSingleton<IRateLimitService, RedisRateLimitService>();
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<IVaultSecretReader, VaultSecretReader>();
        services.AddSingleton<IGrantTokenService, VaultBackedGrantTokenService>();
        services.AddSingleton<IGrantProtector, GrantProtector>();
        services.AddSingleton<IPolicyEvaluator, FoundationPolicyEvaluator>();
        services.AddSingleton<IKafkaSecurityEventProducer, KafkaSecurityEventProducer>();

        services.AddSingleton<IAuthLoginClient>(sp => new AuthLoginRestClient(CreateHttpClient(sp.GetRequiredService<SecurityIntegrationOptions>().AuthLogin), sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<IJwtTokenClient>(sp => new JwtTokenGrpcFacadeClient(sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<IPasskeyClient>(sp => new PasskeyGrpcFacadeClient(sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<ISmartOtpClient>(sp => new SmartOtpGrpcMtlsClient(sp.GetRequiredService<SecurityIntegrationOptions>()));

        return services;
    }


    private static ConfigurationOptions BuildRedisConfiguration(SecurityIntegrationOptions options, IVaultSecretReader secrets)
    {
        var redis = options.Redis;
        var configurationText = RenderedSecretFile.ReadTrimmed(redis.ConfigurationFile)
            ?? redis.Configuration
            ?? string.Empty;
        var endpointText = redis.BootstrapEndpoints ?? string.Empty;
        var configuration = !string.IsNullOrWhiteSpace(configurationText)
            ? ConfigurationOptions.Parse(configurationText)
            : ConfigurationOptions.Parse(endpointText);

        configuration.AbortOnConnectFail = redis.AbortOnConnectFail;
        configuration.ConnectTimeout = redis.ConnectTimeoutMs;
        configuration.SyncTimeout = redis.OperationTimeoutMs;
        configuration.AsyncTimeout = redis.OperationTimeoutMs;
        configuration.DefaultDatabase = 0;
        configuration.Ssl = redis.TlsEnabled;

        if (string.IsNullOrWhiteSpace(configuration.Password) && !string.IsNullOrWhiteSpace(redis.SecretPath))
        {
            var password = RenderedSecretFile.ReadTrimmed(redis.PasswordFile)
                ?? secrets.ReadFieldAsync(new VaultSecretReference(redis.SecretPath, redis.PasswordField), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(password))
            {
                configuration.Password = password;
            }
        }

        return configuration;
    }

    private static void ApplyRenderedSecretFiles(SecurityIntegrationOptions options)
    {
        options.Redis.Configuration = RenderedSecretFile.ReadTrimmed(options.Redis.ConfigurationFile) ?? options.Redis.Configuration;
        options.Redis.KeyPrefix = RenderedSecretFile.ReadTrimmed(options.Redis.KeyPrefixFile) ?? options.Redis.KeyPrefix;
        options.Kafka.BootstrapServers = RenderedSecretFile.ReadTrimmed(options.Kafka.BootstrapServersFile) ?? options.Kafka.BootstrapServers;
    }

    private static HttpClient CreateHttpClient(ChildAppClientOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute),
            Timeout = Timeout.InfiniteTimeSpan
        };
    }
}
