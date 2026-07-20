namespace XanhNow.Security.Application.Abstractions.Context;

public interface ICorrelationContextAccessor
{
    CorrelationContext Current { get; }
}
