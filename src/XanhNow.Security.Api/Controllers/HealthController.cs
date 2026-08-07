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
    private readonly SecurityApiOptions _options;

    public HealthController(IOptions<SecurityApiOptions> options)
    {
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
        var health = HttpContext.RequestServices.GetRequiredService<SecurityDependencyHealthService>();
        return Ok(await health.CheckReadyAsync(_options.ServiceName, cancellationToken));
    }

    [HttpGet(ApiRoutes.Health.Dependencies)]
    public async Task<ActionResult<ReadyHealthResponse>> Dependencies(CancellationToken cancellationToken)
    {
        var health = HttpContext.RequestServices.GetRequiredService<SecurityDependencyHealthService>();
        return Ok(await health.CheckReadyAsync(_options.ServiceName, cancellationToken));
    }
}
