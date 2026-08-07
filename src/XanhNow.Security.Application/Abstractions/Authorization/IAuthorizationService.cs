using XanhNow.Security.Application.Abstractions.Context;

namespace XanhNow.Security.Application.Abstractions.Authorization;

public interface IAuthorizationService
{
    ValueTask<bool> HasPermissionAsync(CallerContext caller, string permission, CancellationToken cancellationToken);
}
public sealed class CallerPermissionAuthorizationService : IAuthorizationService
{
    public ValueTask<bool> HasPermissionAsync(CallerContext caller, string permission, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allowed = caller.IsAuthenticated && caller.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        return ValueTask.FromResult(allowed);
    }
}
