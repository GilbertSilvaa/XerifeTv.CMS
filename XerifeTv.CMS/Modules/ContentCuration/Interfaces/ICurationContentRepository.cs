using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.ContentCuration.Interfaces;

public interface ICurationContentRepository
{
    Task<IReadOnlyList<MovieEntity>> GetMoviesByIdsAsync(IEnumerable<string> ids);
    Task<IReadOnlyList<SeriesEntity>> GetSeriesByIdsAsync(IEnumerable<string> ids);
    Task<IReadOnlyList<MovieEntity>> GetMoviesAsync(DynamicCollectionQuery query);
    Task<IReadOnlyList<SeriesEntity>> GetSeriesAsync(DynamicCollectionQuery query);
    Task<long> CountMoviesAsync(DynamicCollectionQuery query);
    Task<long> CountSeriesAsync(DynamicCollectionQuery query);
    Task<IReadOnlyList<MovieEntity>> SearchMoviesAsync(string term, int limit, bool includeDisabled = true);
    Task<IReadOnlyList<SeriesEntity>> SearchSeriesAsync(string term, int limit, bool includeDisabled = true);
}
