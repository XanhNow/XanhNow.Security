using System.Net.Sockets;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.V1.Health;
using XanhNow.Security.Infrastructure.Integration.Options;
using XanhNow.Security.Infrastructure.Integration.Redis;
using XanhNow.Security.Infrastructure.Integration.Vault;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.Api.Health;

public sealed class SecurityDependencyHealthService
{
    private readonly IServiceProvider _services;
    private readonly SecurityIntegrationOptions _options;

    public SecurityDependencyHealthService(IServiceProvider services, SecurityIntegrationOptions options)
    {
        _services = services;
        _options = options;
    }

    public async Task<ReadyHealthResponse> CheckReadyAsync(string serviceName, CancellationToken cancellationToken)
    {
        var dependencies = new[]
        {
            await CheckPostgresAsync(cancellationToken),
            await CheckVaultAsync(cancellationToken),
            await CheckRedisAsync(cancellationToken),
            await CheckKafkaAsync(cancellationToken),
            await CheckChildAppsAsync(cancellationToken)
        };

        var status = dependencies.Any(x => x.Status == DependencyStatusContract.Unhealthy)
            ? DependencyStatusContract.Unhealthy
            : dependencies.Any(x => x.Status == DependencyStatusContract.Degraded)
                ? DependencyStatusContract.Degraded
                : DependencyStatusContract.Healthy;

        return new ReadyHealthResponse(serviceName, status, dependencies, DateTimeOffset.UtcNow);
    }

    private async Task<DependencyHealthResponse> CheckPostgresAsync(CancellationToken cancellationToken)
    {
        try
        {
            var db = _services.GetRequiredService<SecurityDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect ? Healthy("postgres", "connected") : Unhealthy("postgres", "cannot_connect");
        }
        catch (Exception ex)
        {
            return Unhealthy("postgres", ex.GetType().Name);
        }
    }

    private async Task<DependencyHealthResponse> CheckVaultAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Vault.Address))
            {
                return Unhealthy("vault", "not_configured");
            }

            var vault = _services.GetRequiredService<IVaultSecretReader>();
            var value = await vault.ReadFieldAsync(new VaultSecretReference(_options.Vault.GrantSigningKeyPath, _options.Vault.GrantSigningKeyField), cancellationToken);
            return string.IsNullOrWhiteSpace(value) ? Unhealthy("vault", "grant_key_missing") : Healthy("vault", "grant_key_readable");
        }
        catch (Exception ex)
        {
            return Unhealthy("vault", ex.GetType().Name);
        }
    }

    private async Task<DependencyHealthResponse> CheckRedisAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Redis.Mode, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            return Healthy("redis", "mode=inmemory");
        }

        try
        {
            var endpoints = FirstConfiguredEndpoints(_options.Redis.BootstrapEndpoints, _options.Redis.Configuration);
            var checks = await Task.WhenAll(endpoints.Select(endpoint => CheckEndpointAsync(endpoint.host, endpoint.port, cancellationToken)));
            return checks.Any(x => x)
                ? Healthy("redis", "port_open")
                : Unhealthy("redis", "no_endpoint_open");
        }
        catch (Exception ex)
        {
            return Unhealthy("redis", ex.GetType().Name);
        }
    }

    private Task<DependencyHealthResponse> CheckKafkaAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Kafka.Mode, "Kafka", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Healthy("kafka", "mode=inmemory"));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _options.Kafka.BootstrapServers,
                ClientId = _options.Kafka.ClientId + "-health"
            }).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(2));
            return Task.FromResult(metadata.Brokers.Count > 0 ? Healthy("kafka", $"brokers={metadata.Brokers.Count}") : Unhealthy("kafka", "no_brokers"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Unhealthy("kafka", ex.GetType().Name));
        }
    }

    private async Task<DependencyHealthResponse> CheckChildAppsAsync(CancellationToken cancellationToken)
    {
        var checks = new[]
        {
            await CheckTcpAsync(_options.AuthLogin, cancellationToken),
            await CheckTcpAsync(_options.Jwt, cancellationToken),
            await CheckTcpAsync(_options.Passkey, cancellationToken),
            await CheckTcpAsync(_options.SmartOtp, cancellationToken)
        };

        return checks.All(x => x.ok)
            ? Healthy("child-app-contracts", "ports_open")
            : Unhealthy("child-app-contracts", string.Join(",", checks.Where(x => !x.ok).Select(x => x.name)));
    }


    private static IReadOnlyCollection<(string host, int port)> FirstConfiguredEndpoints(params string[] endpointLists)
    {
        return endpointLists
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(ParseEndpoint)
            .Where(x => !string.IsNullOrWhiteSpace(x.host) && x.port > 0)
            .ToArray();
    }

    private static (string host, int port) ParseEndpoint(string endpoint)
    {
        var parts = endpoint.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && int.TryParse(parts[1], out var port)
            ? (parts[0], port)
            : (endpoint, 6379);
    }

    private static async Task<bool> CheckEndpointAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(1));
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private static async Task<(string name, bool ok)> CheckTcpAsync(ChildAppClientOptions child, CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(child.BaseAddress, UriKind.Absolute);
            var port = uri.IsDefaultPort ? uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80 : uri.Port;
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(1));
            await client.ConnectAsync(uri.Host, port, timeout.Token);
            return (child.Name, true);
        }
        catch
        {
            return (child.Name, false);
        }
    }

    private static DependencyHealthResponse Healthy(string name, string detail) => new(name, DependencyStatusContract.Healthy, detail);
    private static DependencyHealthResponse Unhealthy(string name, string detail) => new(name, DependencyStatusContract.Unhealthy, detail);
}
