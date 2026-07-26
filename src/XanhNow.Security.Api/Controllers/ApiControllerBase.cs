using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiResponseMetadata CreateMetadata()
    {
        var options = HttpContext.RequestServices.GetRequiredService<IOptions<SecurityApiOptions>>().Value;
        return new ApiResponseMetadata(
            options.ContractVersion,
            HttpContext.TraceIdentifier,
            HttpContext.TraceIdentifier,
            DateTimeOffset.UtcNow);
    }

    protected ActionResult<ApiResponse<T>> OkEnvelope<T>(T data)
    {
        return Ok(new ApiResponse<T>(data, CreateMetadata()));
    }
}
