using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Common.Behaviors;

public sealed class RateLimitBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IRateLimitService _rateLimitService;

    public RateLimitBehavior(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRateLimitedRequest rateLimited)
        {
            var decision = await _rateLimitService.CheckAsync(rateLimited.RateLimitKey, rateLimited.MaxRequests, rateLimited.Window, cancellationToken);
            if (!decision.Allowed)
            {
                return Result<TResponse>.Failure(Error.RateLimited(SecurityErrorCodes.RateLimited, "Rate limit exceeded."));
            }
        }

        return await next(cancellationToken);
    }
}
