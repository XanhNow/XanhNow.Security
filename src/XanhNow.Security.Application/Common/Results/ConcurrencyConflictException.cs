namespace XanhNow.Security.Application.Common.Results;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
