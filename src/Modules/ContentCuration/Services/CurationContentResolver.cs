using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;

namespace XerifeTv.CMS.Modules.ContentCuration.Services;

public interface ICurationContentResolver
{
    Task<IReadOnlyList<ResolvedCurationContent>> ResolveAsync(
        IReadOnlyList<CurationContentReference> references);
}

public sealed class CurationContentResolver(ICurationContentRepository repository) : ICurationContentResolver
{
    public async Task<IReadOnlyList<ResolvedCurationContent>> ResolveAsync(
        IReadOnlyList<CurationContentReference> references)
    {
        if (references.Count == 0) return [];

        var movieIds = references
            .Where(r => r.ContentType == EContentType.MOVIE)
            .Select(r => r.ContentId)
            .Distinct()
            .ToList();

        var seriesIds = references
            .Where(r => r.ContentType == EContentType.SERIES)
            .Select(r => r.ContentId)
            .Distinct()
            .ToList();

        var moviesTask = repository.GetMoviesByIdsAsync(movieIds);
        var seriesTask = repository.GetSeriesByIdsAsync(seriesIds);

        await Task.WhenAll(moviesTask, seriesTask);

        var movies = moviesTask.Result.ToDictionary(x => x.Id);
        var series = seriesTask.Result.ToDictionary(x => x.Id);

        var resolved = new List<ResolvedCurationContent>(references.Count);

        foreach (var reference in references)
        {
            if (reference.ContentType == EContentType.MOVIE &&
                movies.TryGetValue(reference.ContentId, out var movie))
            {
                resolved.Add(new(reference.ContentId, EContentType.MOVIE, movie, null));
            }
            else if (reference.ContentType == EContentType.SERIES &&
                     series.TryGetValue(reference.ContentId, out var item))
            {
                resolved.Add(new(reference.ContentId, EContentType.SERIES, null, item));
            }
        }

        return resolved;
    }
}
