namespace XanhNow.Security.Application.Common.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Authentication(string code, string message) => new(code, message, ErrorType.Authentication);
    public static Error Authorization(string code, string message) => new(code, message, ErrorType.Authorization);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error RateLimited(string code, string message) => new(code, message, ErrorType.RateLimited);
    public static Error PolicyDenied(string code, string message) => new(code, message, ErrorType.PolicyDenied);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error DownstreamUnavailable(string code, string message) => new(code, message, ErrorType.DownstreamUnavailable);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
