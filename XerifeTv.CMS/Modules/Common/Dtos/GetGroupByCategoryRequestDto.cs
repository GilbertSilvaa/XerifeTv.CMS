namespace XerifeTv.CMS.Modules.Common.Dtos;

public record GetGroupByCategoryRequestDto(
	ICollection<string> Categories,
	int CurrentPage,
	int LimitResults);