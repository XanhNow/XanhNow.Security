using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/passkeys")]
public sealed class PasskeysController : ApiControllerBase
{
    // RB08 chỉ tạo controller shell. Action nghiệp vụ được mở ở RB12-RB14 theo maturity gate.
}
