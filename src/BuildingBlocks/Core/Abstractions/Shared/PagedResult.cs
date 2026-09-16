namespace BuildingBlocks.Core.Abstractions.Shared;

public class PagedResult<T>
{
    public const int UpperPageSize = 100;
    public const int DefaultPageSize = 10;
    public const int DefaultPageIndex = 1;

    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public bool HasNextPage => PageIndex * PageSize < TotalCount;
    public bool HasPreviousPage => PageIndex > 1;

    public PagedResult(IReadOnlyList<T> items, int pageIndex, int pageSize, int totalCount)
    {
        Items = items;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int pageIndex,
        int pageSize,
        int totalCount)
    {
        return new(items, pageIndex, pageSize, totalCount);
    }
}