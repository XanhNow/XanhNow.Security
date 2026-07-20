using Microsoft.AspNetCore.Mvc;

namespace XanhNow.Security.Api.Controllers;

[ApiController]
[Route("api/security/bootstrap")]
public sealed class BootstrapController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { service = "XanhNow.Security", status = "Bootstrap" });
    }
}
