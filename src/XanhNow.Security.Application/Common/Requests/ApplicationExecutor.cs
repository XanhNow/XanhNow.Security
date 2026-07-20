using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Requests;

public sealed class ApplicationExecutor<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    private readonly IReadOnlyList<IApplicationBehavior<TRequest, TResponse>> _behaviors;

    public ApplicationExecutor(IRequestHandler<TRequest, TResponse> handler, IEnumerable<IApplicationBehavior<TRequest, TResponse>> behaviors)
    {
        _handler = handler;
        _behaviors = behaviors.ToArray();
    }

    public Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationHandlerDelegate<TResponse> next = ct => _handler.HandleAsync(request, ct);

        for (var i = _behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = _behaviors[i];
            var currentNext = next;
            next = ct => behavior.HandleAsync(request, currentNext, ct);
        }

        return next(cancellationToken);
    }
}
