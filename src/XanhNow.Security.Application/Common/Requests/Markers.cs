using XanhNow.Security.Application.Abstractions.Policy;

namespace XanhNow.Security.Application.Common.Requests;

public interface IRequiresAuthenticatedCaller;

public interface IRequiresPermission
{
    string Permission { get; }
}

public interface IRateLimitedRequest
{
    string RateLimitKey { get; }
    int MaxRequests { get; }
    TimeSpan Window { get; }
}

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}

public interface IPolicyRequest
{
    PolicyContext ToPolicyContext();
}

public interface IAuditableRequest
{
    string AuditAction { get; }
}
