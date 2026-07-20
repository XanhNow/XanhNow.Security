using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XanhNow.Security.Application.Abstractions.Caching;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Locking;
using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Infrastructure.Integration.ChildApps.AuthLogin;
using XanhNow.Security.Infrastructure.Integration.ChildApps.Jwt;
using XanhNow.Security.Infrastructure.Integration.ChildApps.Passkey;
using XanhNow.Security.Infrastructure.Integration.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Kafka;
using XanhNow.Security.Infrastructure.Integration.Options;
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
            sp.GetRequiredService<SecurityIntegrationOptionsValidator>().ValidateAndThrow(options);
            return options;
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddSingleton<IRequestFingerprint, JsonRequestFingerprint>();
        services.AddSingleton<RedisRuntimeState>();
        services.AddSingleton<IApplicationCache, RedisApplicationCache>();
        services.AddSingleton<IRateLimitService, RedisRateLimitService>();
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<IVaultSecretReader, VaultSecretReader>();
        services.AddSingleton<IGrantTokenService, VaultBackedGrantTokenService>();
        services.AddSingleton<IKafkaSecurityEventProducer, KafkaSecurityEventProducer>();

        services.AddSingleton<IAuthLoginClient>(sp => new AuthLoginRestClient(CreateHttpClient(sp.GetRequiredService<SecurityIntegrationOptions>().AuthLogin), sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<IJwtTokenClient>(sp => new JwtTokenGrpcFacadeClient(CreateHttpClient(sp.GetRequiredService<SecurityIntegrationOptions>().Jwt), sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<IPasskeyClient>(sp => new PasskeyGrpcFacadeClient(CreateHttpClient(sp.GetRequiredService<SecurityIntegrationOptions>().Passkey), sp.GetRequiredService<SecurityIntegrationOptions>()));
        services.AddSingleton<ISmartOtpClient>(sp => new SmartOtpGrpcMtlsClient(CreateHttpClient(sp.GetRequiredService<SecurityIntegrationOptions>().SmartOtp), sp.GetRequiredService<SecurityIntegrationOptions>()));

        return services;
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
