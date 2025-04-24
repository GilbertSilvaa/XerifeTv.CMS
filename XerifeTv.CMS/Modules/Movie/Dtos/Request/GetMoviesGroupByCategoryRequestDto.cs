namespace XerifeTv.CMS.Modules.Movie.Dtos.Request;

public record GetMoviesGroupByCategoryRequestDto(
	ICollection<string> Categories,
	int CurrentPage,
	int LimitResults);