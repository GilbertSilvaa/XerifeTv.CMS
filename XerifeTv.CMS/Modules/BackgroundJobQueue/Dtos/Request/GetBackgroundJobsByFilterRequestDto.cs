using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public class GetBackgroundJobsByFilterRequestDto(
  EBackgroundJobOrderFilter? order,
  int? limitResults,
  int? currentPage,
  string? responsibleUsername = null,
  EBackgroundJobStatus? status = null)
{
	public string? ResponsibleUsername { get; } = responsibleUsername;
	public EBackgroundJobStatus? Status { get; } = status;
	public EBackgroundJobOrderFilter Order { get; } = order ?? EBackgroundJobOrderFilter.REGISTRATION_DATE_ASC;
	public int LimitResults { get; } = limitResults ?? 1;
	public int CurrentPage { get; } = currentPage ?? 1;
	public string? ResponsibleUserId { get; set; } = null;
}