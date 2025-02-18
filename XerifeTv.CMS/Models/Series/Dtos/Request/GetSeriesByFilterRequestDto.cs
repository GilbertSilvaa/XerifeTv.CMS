using XerifeTv.CMS.Models.Series.Enums;

namespace XerifeTv.CMS.Models.Series.Dtos.Request;

public class GetSeriesByFilterRequestDto(
  ESeriesSearchFilter? filter,
  string? search,
  int? limitResults,
  int? currentPage,
  bool? isIncludeDisabled)
{
  public ESeriesSearchFilter Filter { get; } = filter ?? ESeriesSearchFilter.TITLE;
  public string Search { get; } = search ?? string.Empty;
  public int LimitResults { get; } = limitResults ?? 1;
  public int CurrentPage { get; } = currentPage ?? 1;
  public bool IsIncludeDisabled { get; } = isIncludeDisabled ??  false;
}
