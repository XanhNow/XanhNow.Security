namespace XanhNow.Security.Contracts.Common.Errors;

public static class ApiErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string PolicyDenied = "POLICY_DENIED";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string DependencyTimeout = "DEPENDENCY_TIMEOUT";
    public const string DependencyUnavailable = "DEPENDENCY_UNAVAILABLE";
    public const string OperationAccepted = "OPERATION_ACCEPTED";
    public const string Unexpected = "UNEXPECTED_ERROR";
}
