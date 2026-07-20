namespace XanhNow.Security.Application.Abstractions.Validation;

public sealed record ValidationFailure(string Field, string Code, string Message);
