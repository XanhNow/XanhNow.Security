namespace XanhNow.Security.Application.Abstractions.Persistence;

public sealed record PageRequest(int PageNumber, int PageSize)
{
    public int Skip => (PageNumber - 1) * PageSize;
}

public sealed record PageResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize);
