namespace XanhNow.Security.Application.Abstractions.Context;

public interface ICallerContextAccessor
{
    CallerContext Current { get; }
}
