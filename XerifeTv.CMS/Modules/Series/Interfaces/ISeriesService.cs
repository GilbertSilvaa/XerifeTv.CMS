using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Dtos.Request;
using XerifeTv.CMS.Modules.Series.Dtos.Response;

namespace XerifeTv.CMS.Modules.Series.Interfaces;

public interface ISeriesService
{
    Task<Result<PagedList<GetSeriesResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<GetSeriesResponseDto?>> GetAsync(string id);
	Task<Result<GetSeriesResponseDto?>> GetByImdbIdAsync(string imdbId);
	Task<Result<string>> CreateAsync(CreateSeriesRequestDto dto);
    Task<Result<string>> UpdateAsync(UpdateSeriesRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<PagedList<GetSeriesResponseDto>>> GetByFilterAsync(GetSeriesByFilterRequestDto dto);
    Task<Result<GetEpisodesResponseDto>> GetEpisodesBySeasonAsync(string serieId, int season, bool includeDisabled);
    Task<Result<string>> CreateEpisodeAsync(CreateEpisodeRequestDto dto);
    Task<Result<string>> UpdateEpisodeAsync(UpdateEpisodeRequestDto dto);
    Task<Result<bool>> DeleteEpisodeAsync(string serieId, string id);
}
