namespace XanhNow.Security.Application.Common.Results;

public sealed class Result<TValue>
{
    private Result(TValue? value, Error? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public TValue? Value { get; }
    public Error? Error { get; }

    public static Result<TValue> Success(TValue value) => new(value, null, true);
    public static Result<TValue> Failure(Error error) => new(default, error, false);
}
