using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Series;

namespace XerifeTv.CMS.Modules.ContentCuration.Services;

public interface ICurationCollectionResolver
{
    Task<IReadOnlyList<ResolvedCurationContent>> ResolveItemsAsync(CurationCollectionEntity collection);
    Task<CurationPageResult> ResolvePageAsync(CurationCollectionEntity collection, int page, int pageSize);
}

public sealed class CurationCollectionResolver(
    ICurationContentRepository contentRepository,
    ICurationContentResolver contentResolver) : ICurationCollectionResolver
{
    public Task<IReadOnlyList<ResolvedCurationContent>> ResolveItemsAsync(CurationCollectionEntity collection)
    {
        var limit = Math.Max(1, collection.Limit);

        return collection.Mode == ECurationCollectionMode.STATIC
            ? ResolveStaticAsync(collection.Items, limit)
            : ResolveDynamicAsync(collection, limit);
    }

    private async Task<IReadOnlyList<ResolvedCurationContent>> ResolveDynamicAsync(
        CurationCollectionEntity collection, int limit)
    {
        var types = collection.ContentTypes.Distinct().ToList();
        if (types.Count == 0) return [];

        var query = new DynamicCollectionQuery(
            collection.Filters,
            collection.LogicalOperator,
            collection.OrderBy,
            collection.SortDirection,
            limit);

        var moviesTask = types.Contains(EContentType.MOVIE)
            ? contentRepository.GetMoviesAsync(query)
            : Task.FromResult<IReadOnlyList<MovieEntity>>([]);

        var seriesTask = types.Contains(EContentType.SERIES)
            ? contentRepository.GetSeriesAsync(query)
            : Task.FromResult<IReadOnlyList<SeriesEntity>>([]);

        await Task.WhenAll(moviesTask, seriesTask);

        var candidates = new List<ResolvedCurationContent>(
            moviesTask.Result.Count + seriesTask.Result.Count);

        candidates.AddRange(moviesTask.Result
            .Select(m => new ResolvedCurationContent(m.Id, EContentType.MOVIE, m, null)));

        candidates.AddRange(seriesTask.Result
            .Select(s => new ResolvedCurationContent(s.Id, EContentType.SERIES, null, s)));

        var ordered = OrderCandidates(candidates, collection.OrderBy, collection.SortDirection);

        return ordered.Take(limit).ToList();
    }

    public async Task<IReadOnlyList<ResolvedCurationContent>> ResolveStaticAsync(
        IEnumerable<CollectionItem> items, int limit)
    {
        var references = items
            .OrderBy(x => x.Order)
            .Select(x => new CurationContentReference(x.ContentId, x.ContentType))
            .ToList();

        var resolved = await contentResolver.ResolveAsync(references);
        return resolved.Take(limit).ToList();
    }

    public async Task<CurationPageResult> ResolvePageAsync(
        CurationCollectionEntity collection, int page, int pageSize)
    {
        page = Math.Max(1, page);

        if (collection.Mode == ECurationCollectionMode.STATIC)
        {
            var staticItems = await ResolveStaticAsync(collection.Items, Math.Max(1, collection.Limit));
            return new(staticItems, staticItems.Count, 1, staticItems.Count);
        }

        var size = pageSize > 0 ? pageSize : collection.Limit;
        if (size <= 0) size = CurationConstants.DefaultCollectionLimit;

        var types = collection.ContentTypes.Distinct().ToList();

        var query = new DynamicCollectionQuery(
            collection.Filters,
            collection.LogicalOperator,
            collection.OrderBy,
            collection.SortDirection,
            size);

        if (types.Count == 0)
            return new([], 0, page, size);

        var moviesCountTask = types.Contains(EContentType.MOVIE)
            ? contentRepository.CountMoviesAsync(query)
            : Task.FromResult(0L);

        var seriesCountTask = types.Contains(EContentType.SERIES)
            ? contentRepository.CountSeriesAsync(query)
            : Task.FromResult(0L);

        await Task.WhenAll(moviesCountTask, seriesCountTask);

        var total = moviesCountTask.Result + seriesCountTask.Result;
        var skip = (long)(page - 1) * size;

        if (skip >= total)
            return new([], total, page, size);

        var fetch = (int)Math.Min(total, skip + size);
        var pageQuery = query with { Limit = fetch };

        var moviesTask = types.Contains(EContentType.MOVIE)
            ? contentRepository.GetMoviesAsync(pageQuery)
            : Task.FromResult<IReadOnlyList<MovieEntity>>([]);

        var seriesTask = types.Contains(EContentType.SERIES)
            ? contentRepository.GetSeriesAsync(pageQuery)
            : Task.FromResult<IReadOnlyList<SeriesEntity>>([]);

        await Task.WhenAll(moviesTask, seriesTask);

        var candidates = new List<ResolvedCurationContent>(
            moviesTask.Result.Count + seriesTask.Result.Count);

        candidates.AddRange(moviesTask.Result
            .Select(m => new ResolvedCurationContent(m.Id, EContentType.MOVIE, m, null)));

        candidates.AddRange(seriesTask.Result
            .Select(s => new ResolvedCurationContent(s.Id, EContentType.SERIES, null, s)));

        var items = OrderCandidates(candidates, collection.OrderBy, collection.SortDirection)
            .Skip((int)skip)
            .Take(size)
            .ToList();

        return new(items, total, page, size);
    }

    private static IEnumerable<ResolvedCurationContent> OrderCandidates(
        List<ResolvedCurationContent> candidates,
        ECurationOrderBy orderBy,
        ECurationSortDirection direction)
    {
        var ascending = direction == ECurationSortDirection.ASC;

        IOrderedEnumerable<ResolvedCurationContent> ordered = orderBy switch
        {
            ECurationOrderBy.TITLE => ascending
                ? candidates.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                : candidates.OrderByDescending(x => x.Title, StringComparer.OrdinalIgnoreCase),

            ECurationOrderBy.RELEASE_YEAR => ascending
                ? candidates.OrderBy(x => x.ReleaseYear)
                : candidates.OrderByDescending(x => x.ReleaseYear),

            ECurationOrderBy.REVIEW => ascending
                ? candidates.OrderBy(x => x.Review)
                : candidates.OrderByDescending(x => x.Review),

            _ => ascending
                ? candidates.OrderBy(x => x.CreateAt)
                : candidates.OrderByDescending(x => x.CreateAt)
        };

        return ordered.ThenBy(x => x.ContentId, StringComparer.Ordinal);
    }
}
