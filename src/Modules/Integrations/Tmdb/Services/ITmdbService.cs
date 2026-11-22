using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public interface ITmdbService
{
    Task<Result<GetAllResultsByImdbIdResponseDto?>> GetAllResultsByImdbIdAsync(string imdbId);
    Task<Result<GetMovieByImdbResponseDto?>> GetMovieByImdbIdAsync(string imdbId);
    Task<Result<GetSeriesByImdbResponseDto?>> GetSeriesByImdbIdAsync(string imdbId);
    Task<Result<GetSeriesByImdbResponseDto?>> GetSeriesByTmdbIdAsync(int tmdbId);
    Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesBySeasonAsync(string imdbId, int season);

    Task<Result<IReadOnlyCollection<TmdbSearchResultDto>>> SearchMoviesAsync(string query, int page = 1);
    Task<Result<IReadOnlyCollection<TmdbSearchResultDto>>> SearchSeriesAsync(string query, int page = 1);
    Task<Result<string?>> GetMovieExternalIdsAsync(int tmdbId);
    Task<Result<string?>> GetSeriesExternalIdsAsync(int tmdbId);
    Task<Result<GetSeriesEpisodesBySeasonResponseDto?>> GetSeriesEpisodesByTmdbIdAsync(int tmdbId, int season);
}
