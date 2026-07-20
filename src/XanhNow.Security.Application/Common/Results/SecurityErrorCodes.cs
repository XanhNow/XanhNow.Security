namespace XanhNow.Security.Application.Common.Results;

public static class SecurityErrorCodes
{
    public const string ValidationFailed = "security.validation_failed";
    public const string CallerRequired = "security.caller_required";
    public const string PermissionDenied = "security.permission_denied";
    public const string RateLimited = "security.rate_limited";
    public const string IdempotencyConflict = "security.idempotency_conflict";
    public const string PolicyDenied = "security.policy_denied";
    public const string DownstreamUnavailable = "security.downstream_unavailable";
    public const string OperationNotFound = "security.operation_not_found";
    public const string Unexpected = "security.unexpected";
}
