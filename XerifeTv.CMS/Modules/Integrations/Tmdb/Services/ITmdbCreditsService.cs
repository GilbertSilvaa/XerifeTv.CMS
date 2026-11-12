using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public interface ITmdbCreditsService
{
    Task<Result<TmdbCreditsResponseDto>> GetMovieCreditsAsync(int tmdbId);
    Task<Result<TmdbCreditsResponseDto>> GetSeriesCreditsAsync(int tmdbId);
}
