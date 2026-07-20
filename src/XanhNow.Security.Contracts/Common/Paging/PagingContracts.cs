namespace XanhNow.Security.Contracts.Common.Paging;

public sealed record PageRequest(int Page, int PageSize);
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long TotalCount, bool HasNextPage);
