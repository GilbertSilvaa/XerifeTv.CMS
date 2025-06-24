using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public class GetBackgroundJobsByFilterRequestDto(
  EBackgroundJobSearchFilter? filter,
  string? search,
  int? limitResults,
  int? currentPage)
{
	public EBackgroundJobSearchFilter Filter { get; } = filter ?? EBackgroundJobSearchFilter.REQUESTING_USER;
	public string Search { get; } = search ?? string.Empty;
	public int LimitResults { get; } = limitResults ?? 1;
	public int CurrentPage { get; } = currentPage ?? 1;
}