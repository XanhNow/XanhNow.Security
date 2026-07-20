using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Requests;

public delegate Task<Result<TResponse>> ApplicationHandlerDelegate<TResponse>(CancellationToken cancellationToken);

public interface IApplicationBehavior<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
