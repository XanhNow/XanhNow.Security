using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Domain.Common;

namespace XanhNow.Security.Application.Common.Behaviors;

public interface IApplicationExceptionReporter
{
    ValueTask ReportAsync(Exception exception, CancellationToken cancellationToken);
}

public sealed class ExceptionMappingBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IApplicationExceptionReporter _reporter;

    public ExceptionMappingBehavior(IApplicationExceptionReporter reporter)
    {
        _reporter = reporter;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<TResponse>.Failure(Error.Conflict(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            await _reporter.ReportAsync(ex, cancellationToken);
            return Result<TResponse>.Failure(Error.Unexpected(SecurityErrorCodes.Unexpected, "Unexpected application error."));
        }
    }
}
