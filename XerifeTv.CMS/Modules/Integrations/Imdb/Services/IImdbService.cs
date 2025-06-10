using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Services;

public interface IImdbService
{
    Task<Result<GetAllResultsByImdbIdResponseDto?>> GetAllResultsByImdbIdAsync(string imdbId);
    Task<Result<GetMovieByImdbResponseDto?>> GetMovieByImdbIdAsync(string imdbId);
    Task<Result<GetSeriesByImdbResponseDto?>> GetSeriesByImdbIdAsync(string imdbId);
    Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesBySeasonAsync(string imdbId, int season);
}