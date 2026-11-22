namespace XerifeTv.CMS.Modules.Common;

public sealed class PagedList<T>(
  int currentPage,
  long totalPageCount,
  IEnumerable<T> items) where T : class
{
    public int PageSize => Items.Count();
    public int CurrentPage { get; private set; } = currentPage;
    public long TotalPageCount { get; private set; } = totalPageCount;
    public IEnumerable<T> Items { get; private set; } = items;
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPageCount;
}
