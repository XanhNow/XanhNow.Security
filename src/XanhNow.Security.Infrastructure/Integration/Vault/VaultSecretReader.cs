using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Vault;

public sealed record VaultSecretReference(string Path, string Field);

public interface IVaultSecretReader
{
    ValueTask<string?> ReadFieldAsync(VaultSecretReference reference, CancellationToken cancellationToken);
}

internal sealed class VaultSecretReader : IVaultSecretReader
{
    private readonly HttpClient _http;
    private readonly VaultIntegrationOptions _options;
    private string? _clientToken;

    public VaultSecretReader(SecurityIntegrationOptions options)
    {
        _options = options.Vault;
        if (string.IsNullOrWhiteSpace(_options.Address))
        {
            throw new InvalidOperationException("Vault Address is required.");
        }

        _http = string.IsNullOrWhiteSpace(FirstNonEmpty(_options.CaCertFile, _options.CaCertificatePath))
            ? new HttpClient()
            : CreateHttpClient(_options);
        _http.BaseAddress = new Uri(_options.Address.TrimEnd('/') + "/");
    }

    public async ValueTask<string?> ReadFieldAsync(VaultSecretReference reference, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference.Field);

        var token = await GetTokenAsync(cancellationToken);
        var path = NormalizeKvDataPath(reference.Path);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{path}");
        request.Headers.Add("X-Vault-Token", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        await EnsureVaultSuccessAsync(response, $"read secret '{reference.Path}'", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        var data = payload?["data"]?["data"]?.AsObject()
            ?? throw new InvalidOperationException("Vault KV v2 response did not contain a data object.");

        return data.TryGetPropertyValue(reference.Field, out var node) && node is not null
            ? node.GetValue<string>()
            : null;
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_clientToken))
        {
            return _clientToken;
        }

        var roleId = await ReadConfiguredSecretAsync(
            Environment.GetEnvironmentVariable(_options.RoleIdEnvironmentVariable),
            _options.RoleIdFile,
            cancellationToken);
        var secretId = await ReadConfiguredSecretAsync(
            Environment.GetEnvironmentVariable(_options.SecretIdEnvironmentVariable),
            _options.SecretIdFile,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(secretId))
        {
            throw new InvalidOperationException(
                "Vault AppRole material is not available. " +
                $"Resolved RoleIdFile='{_options.RoleIdFile}', exists={File.Exists(_options.RoleIdFile)}; " +
                $"SecretIdFile='{_options.SecretIdFile}', exists={File.Exists(_options.SecretIdFile)}.");
        }

        var authMount = string.IsNullOrWhiteSpace(_options.AuthMount) ? "approle" : _options.AuthMount.Trim('/');
        using var response = await _http.PostAsJsonAsync($"/v1/auth/{authMount}/login", new
        {
            role_id = roleId,
            secret_id = secretId
        }, cancellationToken);

        await EnsureVaultSuccessAsync(response, "AppRole login", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        _clientToken = payload?["auth"]?["client_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Vault AppRole login did not return a client token.");
        return _clientToken;
    }

    private static string NormalizeKvDataPath(string path)
    {
        var trimmed = path.Trim('/');
        if (trimmed.Contains("/data/", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var parts = trimmed.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("Vault KV v2 path must include mount and secret path.");
        }

        return $"{parts[0]}/data/{parts[1]}";
    }

    private static async Task<string> ReadConfiguredSecretAsync(string? value, string file, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return string.IsNullOrWhiteSpace(file) || !File.Exists(file)
            ? string.Empty
            : (await File.ReadAllTextAsync(file, cancellationToken)).Trim();
    }

    private static async Task EnsureVaultSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        throw new HttpRequestException($"Vault {operation} failed with {status}. Response: {body}", null, response.StatusCode);
    }

    private static HttpClient CreateHttpClient(VaultIntegrationOptions options)
    {
        var caPath = FirstNonEmpty(options.CaCertFile, options.CaCertificatePath);
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
        {
            if (certificate is null || string.IsNullOrWhiteSpace(caPath))
            {
                return false;
            }

            using var ca = X509CertificateLoader.LoadCertificateFromFile(caPath);
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain.Build(certificate);
        };

        return new HttpClient(handler);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
