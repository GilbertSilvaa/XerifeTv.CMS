using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;

namespace XerifeTv.CMS.Modules.Movie.Interfaces;

public interface IMovieService
{
  Task<Result<PagedList<GetMovieResponseDto>>> Get(int currentPage, int limit);
  Task<Result<GetMovieResponseDto?>> Get(string id);
  Task<Result<string>> Create(CreateMovieRequestDto dto);
  Task<Result<string>> Update(UpdateMovieRequestDto dto);
  Task<Result<bool>> Delete(string id);
  Task<Result<PagedList<GetMovieResponseDto>>> GetByFilter(GetMoviesByFilterRequestDto dto);
  Task<Result<GetMovieByImdbResponseDto?>> GetByImdbId(string imdbId);
  Task<Result<(int? SuccessCount, int? FailCount, string[] ErrorList)>> RegisterBySpreadsheet(IFormFile file);
}
