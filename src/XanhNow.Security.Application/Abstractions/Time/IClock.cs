namespace XanhNow.Security.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
