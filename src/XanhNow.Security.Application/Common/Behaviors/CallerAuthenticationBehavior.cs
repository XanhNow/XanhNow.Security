using XanhNow.Security.Application.Abstractions.Context;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class CallerAuthenticationBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly ICallerContextAccessor _callerContext;

    public CallerAuthenticationBehavior(ICallerContextAccessor callerContext)
    {
        _callerContext = callerContext;
    }

    public Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequiresAuthenticatedCaller && !_callerContext.Current.IsAuthenticated)
        {
            return Task.FromResult(Result<TResponse>.Failure(Error.Authentication(SecurityErrorCodes.CallerRequired, "Authenticated caller is required.")));
        }

        return next(cancellationToken);
    }
}
