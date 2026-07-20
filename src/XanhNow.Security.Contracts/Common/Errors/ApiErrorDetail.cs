namespace XanhNow.Security.Contracts.Common.Errors;

public sealed record ApiErrorDetail(string Code, string Message, string? Field = null);
