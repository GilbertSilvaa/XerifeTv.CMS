using XerifeTv.CMS.Modules.Channel.Enums;

namespace XerifeTv.CMS.Modules.Channel.Dtos.Request;

public class GetChannelsByFilterRequestDto(
  EChannelSearchFilter? filter,
  string? search,
  int? limitResults,
  int? currentPage,
  bool? isIncludeDisabled)
{
  public EChannelSearchFilter Filter { get; } = filter ?? EChannelSearchFilter.TITLE;
  public string Search { get; } = search ?? string.Empty;
  public int LimitResults { get; } = limitResults ?? 1;
  public int CurrentPage { get; } = currentPage ?? 1;
  public bool IsIncludeDisabled { get; } = isIncludeDisabled ??  false;
}
