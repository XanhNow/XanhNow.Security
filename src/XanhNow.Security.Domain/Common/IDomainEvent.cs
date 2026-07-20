namespace XanhNow.Security.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
