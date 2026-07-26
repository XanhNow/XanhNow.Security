using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/policies")]
public sealed class PolicyController : ApiControllerBase
{
    // RB08 chỉ tạo controller shell. Action nghiệp vụ được mở ở RB12-RB14 theo maturity gate.
}
