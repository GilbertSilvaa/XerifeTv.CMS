using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;

namespace XerifeTv.CMS.Modules.Movie.Interfaces;

public interface IMovieService
{
    Task<Result<PagedList<GetMovieResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<GetMovieResponseDto?>> GetAsync(string id);
	Task<Result<GetMovieResponseDto?>> GetByImdbIdAsync(string imdbId);
	Task<Result<string>> CreateAsync(CreateMovieRequestDto dto);
    Task<Result<string>> UpdateAsync(UpdateMovieRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<PagedList<GetMovieResponseDto>>> GetByFilterAsync(GetMoviesByFilterRequestDto dto);
}
