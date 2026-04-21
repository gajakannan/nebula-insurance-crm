namespace Nebula.Application.Common;

public record PaginatedResult<T>(
    IReadOnlyList<T> Data,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
