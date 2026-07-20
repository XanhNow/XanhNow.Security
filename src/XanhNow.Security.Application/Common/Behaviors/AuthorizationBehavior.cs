using XanhNow.Security.Application.Abstractions.Authorization;
using XanhNow.Security.Application.Abstractions.Context;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly ICallerContextAccessor _callerContext;
    private readonly IAuthorizationService _authorization;

    public AuthorizationBehavior(ICallerContextAccessor callerContext, IAuthorizationService authorization)
    {
        _callerContext = callerContext;
        _authorization = authorization;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequiresPermission permissionRequest)
        {
            var allowed = await _authorization.HasPermissionAsync(_callerContext.Current, permissionRequest.Permission, cancellationToken);
            if (!allowed)
            {
                return Result<TResponse>.Failure(Error.Authorization(SecurityErrorCodes.PermissionDenied, "Caller does not have required permission."));
            }
        }

        return await next(cancellationToken);
    }
}
