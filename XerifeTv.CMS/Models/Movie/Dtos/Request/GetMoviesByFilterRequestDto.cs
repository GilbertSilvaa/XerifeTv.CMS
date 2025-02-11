using XerifeTv.CMS.Models.Movie.Enums;

namespace XerifeTv.CMS.Models.Movie.Dtos.Request;

public class GetMoviesByFilterRequestDto(
  EMovieSearchFilter? filter,
  EMovieOrderFilter? order,
  string? search, 
  int? limitResults, 
  int? currentPage)
{
  public EMovieSearchFilter Filter { get; } = filter ?? EMovieSearchFilter.TITLE;
  public EMovieOrderFilter Order { get; } = order ?? EMovieOrderFilter.TITLE;
  public string Search { get; } = search ?? string.Empty;
  public int LimitResults { get; } = limitResults ?? 1;
  public int CurrentPage { get; } = currentPage ?? 1;
}
