namespace XanhNow.Security.Application.Abstractions.RateLimiting;

public interface IRateLimitService
{
    ValueTask<RateLimitDecision> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken);
}
