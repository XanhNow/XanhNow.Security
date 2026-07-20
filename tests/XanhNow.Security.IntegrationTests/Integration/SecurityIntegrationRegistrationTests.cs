using Microsoft.Extensions.DependencyInjection;
using XanhNow.Security.Application.Abstractions.Caching;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Application.Abstractions.Locking;
using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Infrastructure.Integration;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.IntegrationTests.Integration;

public sealed class SecurityIntegrationRegistrationTests
{
    [Fact]
    public void AddSecurityIntegration_RegistersRequiredApplicationPorts()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IAuthLoginClient>());
        Assert.NotNull(provider.GetRequiredService<IJwtTokenClient>());
        Assert.NotNull(provider.GetRequiredService<IPasskeyClient>());
        Assert.NotNull(provider.GetRequiredService<ISmartOtpClient>());
        Assert.NotNull(provider.GetRequiredService<IApplicationCache>());
        Assert.NotNull(provider.GetRequiredService<IRateLimitService>());
        Assert.NotNull(provider.GetRequiredService<IIdempotencyStore>());
        Assert.NotNull(provider.GetRequiredService<IDistributedLockService>());
    }

    [Fact]
    public void OptionsValidator_RejectsInvalidChildBaseAddress()
    {
        var options = ValidOptions();
        options.AuthLogin.BaseAddress = "not-a-uri";

        var errors = new SecurityIntegrationOptionsValidator().Validate(options);

        Assert.Contains(errors, error => error.Contains("BaseAddress", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cache_RoundTripsJsonValueAndHonorsRemove()
    {
        using var provider = BuildProvider();
        var cache = provider.GetRequiredService<IApplicationCache>();

        await cache.SetAsync("profile:u1", new CacheDto("u1", 3), TimeSpan.FromMinutes(1), CancellationToken.None);
        var value = await cache.GetAsync<CacheDto>("profile:u1", CancellationToken.None);
        await cache.RemoveAsync("profile:u1", CancellationToken.None);
        var removed = await cache.GetAsync<CacheDto>("profile:u1", CancellationToken.None);

        Assert.Equal(new CacheDto("u1", 3), value);
        Assert.Null(removed);
    }

    [Fact]
    public async Task RateLimit_DeniesAfterConfiguredWindowLimit()
    {
        using var provider = BuildProvider();
        var rateLimit = provider.GetRequiredService<IRateLimitService>();

        var first = await rateLimit.CheckAsync("login:phone", 1, TimeSpan.FromMinutes(1), CancellationToken.None);
        var second = await rateLimit.CheckAsync("login:phone", 1, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.True(first.Allowed);
        Assert.False(second.Allowed);
        Assert.NotNull(second.RetryAfter);
    }

    [Fact]
    public async Task IdempotencyStore_ReservesAndCompletesRequest()
    {
        using var provider = BuildProvider();
        var store = provider.GetRequiredService<IIdempotencyStore>();

        await store.ReserveAsync("idem-1", "hash-1", CancellationToken.None);
        var reserved = await store.FindAsync("idem-1", CancellationToken.None);
        await store.CompleteAsync("idem-1", "{\"ok\":true}", CancellationToken.None);
        var completed = await store.FindAsync("idem-1", CancellationToken.None);

        Assert.NotNull(reserved);
        Assert.False(reserved.Completed);
        Assert.NotNull(completed);
        Assert.True(completed.Completed);
        Assert.Equal("{\"ok\":true}", completed.StoredResultJson);
    }

    [Fact]
    public async Task DistributedLock_AllowsSingleOwnerUntilReleased()
    {
        using var provider = BuildProvider();
        var locks = provider.GetRequiredService<IDistributedLockService>();

        await using var first = await locks.TryAcquireAsync("user:u1", TimeSpan.FromSeconds(30), CancellationToken.None);
        var second = await locks.TryAcquireAsync("user:u1", TimeSpan.FromSeconds(30), CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSecurityIntegration(options => Copy(ValidOptions(), options));
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static SecurityIntegrationOptions ValidOptions() => new()
    {
        AuthLogin = new ChildAppClientOptions { Name = "Auth_Login_App", BaseAddress = "http://127.0.0.1:8080", Deadline = TimeSpan.FromSeconds(5) },
        Jwt = new ChildAppClientOptions { Name = "JWT_Refresh_Token_App", BaseAddress = "http://127.0.0.1:5102", Deadline = TimeSpan.FromSeconds(5) },
        Passkey = new ChildAppClientOptions { Name = "Passkey_Provider_App", BaseAddress = "http://127.0.0.1:5101", Deadline = TimeSpan.FromSeconds(5) },
        SmartOtp = new ChildAppClientOptions { Name = "SmartOtp_App", BaseAddress = "https://127.0.0.1:5104", Deadline = TimeSpan.FromSeconds(5), RequiresMtls = true, TrustedCaPath = "/etc/xanhnow/auth-smart-otp/mtls/ca/smart-otp-grpc-client-ca.crt" },
        Redis = new RedisIntegrationOptions { KeyPrefix = "xanhnow:security:test", DefaultCacheTtl = TimeSpan.FromMinutes(5), IdempotencyTtl = TimeSpan.FromMinutes(15), LockTtl = TimeSpan.FromSeconds(30) },
        Kafka = new KafkaIntegrationOptions { SecurityEventsTopic = "xanhnow.security.events", SecurityAuditTopic = "xanhnow.security.audit" },
        Vault = new VaultIntegrationOptions { Address = "https://127.0.0.1:8200", RoleIdFile = "/etc/xanhnow/security/vault/role_id", SecretIdFile = "/etc/xanhnow/security/vault/secret_id" }
    };

    private static void Copy(SecurityIntegrationOptions source, SecurityIntegrationOptions target)
    {
        target.AuthLogin = source.AuthLogin;
        target.Jwt = source.Jwt;
        target.Passkey = source.Passkey;
        target.SmartOtp = source.SmartOtp;
        target.Redis = source.Redis;
        target.Kafka = source.Kafka;
        target.Vault = source.Vault;
        target.DefaultDeadline = source.DefaultDeadline;
        target.ContractVersion = source.ContractVersion;
    }

    private sealed record CacheDto(string UserId, int Version);
}
