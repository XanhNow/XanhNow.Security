using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Requests;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
