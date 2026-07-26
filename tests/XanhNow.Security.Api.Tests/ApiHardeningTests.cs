using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts;
using XanhNow.Security.Contracts.Common.Errors;

namespace XanhNow.Security.Api.Tests;

public sealed class ApiHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiHardeningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Security_api_options_expose_production_hardening_defaults()
    {
        using var scope = _factory.Services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptions<SecurityApiOptions>>().Value;

        Assert.True(options.EnableSecurityHeaders);
        Assert.Equal(1_048_576, options.MaxRequestBodyBytes);
        Assert.Equal(32_768, options.MaxRequestHeadersTotalSizeBytes);
        Assert.Equal(10, options.RequestHeadersTimeoutSeconds);
        Assert.Equal(128, options.MaxCorrelationIdLength);
    }

    [Fact]
    public async Task Security_headers_include_rb16_edge_hardening_headers()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(ApiRoutes.Health.Live);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.True(response.Headers.Contains("Cross-Origin-Resource-Policy"));
        Assert.True(response.Headers.Contains("X-Permitted-Cross-Domain-Policies"));
    }

    [Fact]
    public async Task Correlation_id_longer_than_hardening_limit_is_rejected()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Health.Live);
        request.Headers.Add(SecurityHeaders.CorrelationId, new string('x', 129));

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ApiJson.SerializerOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("SECURITY_INVALID_CORRELATION_ID", body?.Code);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Security_api_options_validator_rejects_unsafe_hardening_values()
    {
        var validator = new SecurityApiOptionsValidator();
        var options = new SecurityApiOptions
        {
            MaxRequestHeadersTotalSizeBytes = 4_096,
            RequestHeadersTimeoutSeconds = 0,
            MaxCorrelationIdLength = 8
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("MaxRequestHeadersTotalSizeBytes", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("RequestHeadersTimeoutSeconds", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("MaxCorrelationIdLength", StringComparison.Ordinal));
    }
}
