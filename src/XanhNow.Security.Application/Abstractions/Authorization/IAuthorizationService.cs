using XanhNow.Security.Application.Abstractions.Context;

namespace XanhNow.Security.Application.Abstractions.Authorization;

public interface IAuthorizationService
{
    ValueTask<bool> HasPermissionAsync(CallerContext caller, string permission, CancellationToken cancellationToken);
}
