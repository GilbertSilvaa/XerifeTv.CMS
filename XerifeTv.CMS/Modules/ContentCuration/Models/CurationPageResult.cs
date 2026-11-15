namespace XerifeTv.CMS.Modules.ContentCuration.Models;

public sealed record CurationPageResult(
    IReadOnlyList<ResolvedCurationContent> Items,
    long TotalCount,
    int CurrentPage,
    int PageSize)
{
    public int TotalPageCount => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => CurrentPage > 1;

    public bool HasNext => CurrentPage < TotalPageCount;
}
