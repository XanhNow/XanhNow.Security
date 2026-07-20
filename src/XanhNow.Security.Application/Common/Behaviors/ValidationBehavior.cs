using XanhNow.Security.Application.Abstractions.Validation;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IReadOnlyList<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToArray();
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var validator in _validators)
        {
            var failures = await validator.ValidateAsync(request, cancellationToken);
            var firstFailure = failures.FirstOrDefault();
            if (firstFailure is not null)
            {
                return Result<TResponse>.Failure(Error.Validation(firstFailure.Code, firstFailure.Message));
            }
        }

        return await next(cancellationToken);
    }
}
