using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Health;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts;
using XanhNow.Security.Contracts.V1.Health;

namespace XanhNow.Security.Api.Controllers;

[AllowAnonymous]
public sealed class HealthController : ApiControllerBase
{
    private readonly SecurityDependencyHealthService _health;
    private readonly SecurityApiOptions _options;

    public HealthController(SecurityDependencyHealthService health, IOptions<SecurityApiOptions> options)
    {
        _health = health;
        _options = options.Value;
    }

    [HttpGet(ApiRoutes.Health.Live)]
    public ActionResult<LiveHealthResponse> Live()
    {
        return Ok(new LiveHealthResponse(_options.ServiceName, "Healthy", DateTimeOffset.UtcNow));
    }

    [HttpGet(ApiRoutes.Health.Ready)]
    public async Task<ActionResult<ReadyHealthResponse>> Ready(CancellationToken cancellationToken)
    {
        return Ok(await _health.CheckReadyAsync(_options.ServiceName, cancellationToken));
    }

    [HttpGet(ApiRoutes.Health.Dependencies)]
    public async Task<ActionResult<ReadyHealthResponse>> Dependencies(CancellationToken cancellationToken)
    {
        return Ok(await _health.CheckReadyAsync(_options.ServiceName, cancellationToken));
    }
}
