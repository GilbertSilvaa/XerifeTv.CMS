using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Tmdb.Services;

public interface ITmdbPersonService
{
    Task<Result<IEnumerable<TmdbPersonSearchResultDto>>> SearchAsync(string name);
    Task<Result<TmdbPersonDetailsDto>> GetDetailsAsync(int tmdbId);
}
