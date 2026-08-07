using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using XanhNow.Security.Api;
using XanhNow.Security.Contracts;
using XanhNow.Security.Contracts.Common.Enums;
using XanhNow.Security.Contracts.V1.Health;

namespace XanhNow.Security.Api.Tests;

public sealed class ApiFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiFoundationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_health_returns_healthy_and_correlation_headers()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Health.Live);
        request.Headers.Add(SecurityHeaders.CorrelationId, "rb08-correlation-test");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<LiveHealthResponse>(ApiJson.SerializerOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body?.Status);
        Assert.True(response.Headers.TryGetValues(SecurityHeaders.CorrelationId, out var values));
        Assert.Equal("rb08-correlation-test", Assert.Single(values));
        Assert.True(response.Headers.Contains(SecurityHeaders.RequestId));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Ready_health_returns_redacted_dependency_summary()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<ReadyHealthResponse>(ApiRoutes.Health.Ready, ApiJson.SerializerOptions);

        Assert.NotNull(response);
        Assert.Contains(response!.Status, new[] { DependencyStatusContract.Healthy, DependencyStatusContract.Degraded, DependencyStatusContract.Unhealthy });
        Assert.Contains(response.Dependencies, x => x.Name == "postgres");
        Assert.DoesNotContain(response.Dependencies, x => x.Message.Contains("192.168.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.Dependencies, x => x.Message.Contains("kv/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Public_openapi_contains_health_and_core_vertical_slice_routes()
    {
        using var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/public-v1.json");

        Assert.Contains(ApiRoutes.Health.Live, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Auth.Register, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Auth.RegisterPasskeyBegin, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Auth.RegisterPasskeyFinish, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Auth.PasswordLogin, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Sessions.Refresh, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.Passkeys.RegistrationBegin, json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ApiRoutes.SmartOtp.EnrollBegin, json, StringComparison.OrdinalIgnoreCase);
    }
}
