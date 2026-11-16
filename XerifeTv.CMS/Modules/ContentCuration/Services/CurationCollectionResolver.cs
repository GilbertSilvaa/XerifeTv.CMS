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

        var candidates = await FetchCandidatesAsync(types, query);

        return OrderCandidates(candidates, collection.OrderBy, collection.SortDirection)
            .Take(limit)
            .ToList();
    }

    private async Task<IReadOnlyList<ResolvedCurationContent>> ResolveStaticAsync(
        IEnumerable<CollectionItem> items, int limit)
    {
        limit = Math.Max(1, limit);

        var references = items
            .OrderBy(x => x.Order)
            .Select(x => new CurationContentReference(x.ContentId, x.ContentType))
            .ToList();

        if (references.Count == 0) return [];

        var batchSize = Math.Max(limit, CurationConstants.DefaultCollectionLimit);
        var resolved = new List<ResolvedCurationContent>(Math.Min(limit, references.Count));

        for (var offset = 0; offset < references.Count && resolved.Count < limit; offset += batchSize)
        {
            var batch = references
                .Skip(offset)
                .Take(batchSize)
                .ToList();

            var batchItems = await contentResolver.ResolveAsync(batch);
            resolved.AddRange(batchItems);
        }

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
        if (types.Count == 0) return new([], 0, page, size);

        var skip = (long)(page - 1) * size;
        var forwardWindow = (int)Math.Min(int.MaxValue, skip + size);

        var query = new DynamicCollectionQuery(
            collection.Filters,
            collection.LogicalOperator,
            collection.OrderBy,
            collection.SortDirection,
            forwardWindow);

        return forwardWindow <= CurationConstants.ParallelPaginationWindow
            ? await ResolveShallowPageAsync(collection, types, query, page, size, skip)
            : await ResolveDeepPageAsync(collection, types, query, page, size, skip, forwardWindow);
    }

    private async Task<CurationPageResult> ResolveShallowPageAsync(
        CurationCollectionEntity collection,
        IReadOnlyList<EContentType> types,
        DynamicCollectionQuery query,
        int page,
        int size,
        long skip)
    {
        var countTask = CountTotalAsync(types, query);
        var fetchTask = FetchCandidatesAsync(types, query);

        await Task.WhenAll(countTask, fetchTask);

        var total = countTask.Result;

        if (skip >= total)
            return new([], total, page, size);

        var items = OrderCandidates(fetchTask.Result, collection.OrderBy, collection.SortDirection)
            .Skip((int)skip)
            .Take(size)
            .ToList();

        return new(items, total, page, size);
    }

    private async Task<CurationPageResult> ResolveDeepPageAsync(
        CurationCollectionEntity collection,
        IReadOnlyList<EContentType> types,
        DynamicCollectionQuery query,
        int page,
        int size,
        long skip,
        int forwardWindow)
    {
        var total = await CountTotalAsync(types, query);

        if (skip >= total)
            return new([], total, page, size);

        var tail = total - skip - size;
        var backwardWindow = tail >= 0
            ? (int)Math.Min(int.MaxValue, tail + size)
            : int.MaxValue;

        if (tail < 0 || backwardWindow >= forwardWindow)
        {
            var forwardFetch = (int)Math.Min((long)int.MaxValue, Math.Min(total, skip + size));
            var pageQuery = query with { Limit = forwardFetch };
            var candidates = await FetchCandidatesAsync(types, pageQuery);

            var items = OrderCandidates(candidates, collection.OrderBy, collection.SortDirection)
                .Skip((int)skip)
                .Take(size)
                .ToList();

            return new(items, total, page, size);
        }

        var reverseQuery = query with
        {
            Limit = backwardWindow,
            Reverse = true
        };

        var reverseCandidates = await FetchCandidatesAsync(types, reverseQuery);

        var reverseItems = OrderCandidates(
                reverseCandidates,
                collection.OrderBy,
                collection.SortDirection,
                reverseId: true)
            .Skip((int)tail)
            .Take(size)
            .Reverse()
            .ToList();

        return new(reverseItems, total, page, size);
    }

    private async Task<List<ResolvedCurationContent>> FetchCandidatesAsync(
        IReadOnlyList<EContentType> types,
        DynamicCollectionQuery query)
    {
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

        return candidates;
    }

    private async Task<long> CountTotalAsync(
        IReadOnlyList<EContentType> types,
        DynamicCollectionQuery query)
    {
        var moviesTask = types.Contains(EContentType.MOVIE)
            ? contentRepository.CountMoviesAsync(query)
            : Task.FromResult(0L);

        var seriesTask = types.Contains(EContentType.SERIES)
            ? contentRepository.CountSeriesAsync(query)
            : Task.FromResult(0L);

        await Task.WhenAll(moviesTask, seriesTask);

        return moviesTask.Result + seriesTask.Result;
    }

    private static IEnumerable<ResolvedCurationContent> OrderCandidates(
        List<ResolvedCurationContent> candidates,
        ECurationOrderBy orderBy,
        ECurationSortDirection direction,
        bool reverseId = false)
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

        return reverseId
            ? ordered.ThenByDescending(x => x.ContentId, StringComparer.Ordinal)
            : ordered.ThenBy(x => x.ContentId, StringComparer.Ordinal);
    }
}
