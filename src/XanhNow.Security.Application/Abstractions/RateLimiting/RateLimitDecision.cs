namespace XanhNow.Security.Application.Abstractions.RateLimiting;

public sealed record RateLimitDecision(bool Allowed, TimeSpan? RetryAfter)
{
    public static RateLimitDecision Allow() => new(true, null);
    public static RateLimitDecision Deny(TimeSpan retryAfter) => new(false, retryAfter);
}
