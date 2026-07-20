namespace XanhNow.Security.Application.Abstractions.ChildApps;

public sealed record ChildCallError(string Code, string Message, bool Retryable);

public sealed class ChildCallResult<TValue>
{
    private ChildCallResult(TValue? value, ChildCallError? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public TValue? Value { get; }
    public ChildCallError? Error { get; }

    public static ChildCallResult<TValue> Success(TValue value) => new(value, null, true);
    public static ChildCallResult<TValue> Failure(ChildCallError error) => new(default, error, false);
}

public sealed record SensitiveString(string Value)
{
    public override string ToString() => "[REDACTED]";
}
