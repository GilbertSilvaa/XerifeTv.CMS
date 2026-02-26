using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content.Dtos.Response;

namespace XerifeTv.CMS.Modules.Content.Interfaces;

public interface IContentV2Service
{
    Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesAsync(int limit);
    Task<Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>> GetSeriesAsync(int limit);
    Task<Result<MovieContentV2ResponseDto?>> GetMovieByIdAsync(string id);
    Task<Result<SeriesSummaryContentV2ResponseDto?>> GetSeriesByIdAsync(string id);
    Task<Result<IEnumerable<EpisodeContentV2ResponseDto>>> GetEpisodesBySeriesIdAndSeasonAsync(string seriesId, int seasonNumber);
    Task<Result<string[]>> GetMoviesCategoriesAsync(int limit = 10);
    Task<Result<string[]>> GetSeriesCategoriesAsync(int limit = 10);
    Task<Result<PagedList<ItemsByCategory<MovieContentV2ResponseDto>>>> GetMoviesByCategoryAsync(string category, int page = 1, int pageSize = 1);
    Task<Result<PagedList<ItemsByCategory<SeriesSummaryContentV2ResponseDto>>>> GetSeriesByCategoryAsync(string category, int page = 1, int pageSize = 1);
    Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesRecommendedAsync(string movieId);
    Task<Result<IEnumerable<MovieContentV2ResponseDto>>> GetMoviesByTermAsync(string searchTerm, int limit = 10);
    Task<Result<IEnumerable<SeriesSummaryContentV2ResponseDto>>> GetSeriesByTermAsync(string searchTerm, int limit = 10);
    Task<Result<GetHomeContentV2ResponseDto>> GetHomeContentAsync();
    Task<Result<PagedList<ItemsByCategory<MovieContentV2ResponseDto>>>> GetMoviesByCategoriesListAsync(List<string> categories, int page, int pageSize = 1);
    Task<Result<PagedList<ItemsByCategory<SeriesSummaryContentV2ResponseDto>>>> GetSeriesByCategoriesListAsync(List<string> categories, int page, int pageSize = 1);
}
