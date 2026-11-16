using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Content.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;
using XerifeTv.CMS.Modules.ContentCuration.Services;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationCompositionService(
    IFeaturedRepository featuredRepository,
    ICurationCollectionService collectionService,
    ICurationCollectionResolver collectionResolver,
    ICurationContentResolver contentResolver,
    ICurationSettingsRepository settingsRepository,
    IConfiguration configuration) : ICurationCompositionService
{
    public async Task<Result<GetCurationHomeV2ResponseDto>> GetHomeAsync()
    {
        try
        {
            var activeFeatureds = await featuredRepository.GetOrderedAsync(onlyActive: true);
            var hero = await featuredRepository.GetHomeHeroAsync() ?? activeFeatureds.FirstOrDefault();

            var featureds = new List<FeaturedContent>();

            if (hero is not null)
            {
                var references = hero.Items
                    .OrderBy(x => x.Order)
                    .Take(Math.Max(1, hero.MaxItems))
                    .Select(x => new CurationContentReference(x.ContentId, x.ContentType))
                    .ToList();

                var resolved = await contentResolver.ResolveAsync(references);

                featureds.AddRange(resolved.Select(ToFeaturedContent));
            }

            var categoriesResult = await collectionService.GetActiveCategoryNamesAsync();

            if (categoriesResult.IsFailure)
                return Result<GetCurationHomeV2ResponseDto>.Failure(categoriesResult.Error);

            return Result<GetCurationHomeV2ResponseDto>.Success(new GetCurationHomeV2ResponseDto
            {
                Featureds = featureds,
                Categories = categoriesResult.Data ?? []
            });
        }
        catch (Exception ex)
        {
            return Result<GetCurationHomeV2ResponseDto>.Failure(new("500", ex.Message));
        }
    }

    public async Task<Result<GetCurationCategoryV2ResponseDto?>> GetCategoryAsync(
        string category, int page = 1, int pageSize = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
                return Result<GetCurationCategoryV2ResponseDto?>.Success(null);

            var collectionResult = await collectionService.GetActiveByNameAsync(category);

            if (collectionResult.IsFailure)
                return Result<GetCurationCategoryV2ResponseDto?>.Failure(collectionResult.Error);

            var collection = collectionResult.Data;

            if (collection is null)
                return Result<GetCurationCategoryV2ResponseDto?>.Success(null);

            if (collection.Mode == ECurationCollectionMode.STATIC || !collection.AllowsPagination)
            {
                var resolved = await collectionResolver.ResolveItemsAsync(collection);

                return Result<GetCurationCategoryV2ResponseDto?>.Success(
                    new GetCurationCategoryV2ResponseDto
                    {
                        Category = collection.Name,
                        Items = [.. resolved.Select(ToCategoryItem)]
                    });
            }

            var pageResult = await collectionResolver.ResolvePageAsync(collection, page, pageSize);

            return Result<GetCurationCategoryV2ResponseDto?>.Success(
                new GetCurationCategoryV2ResponseDto
                {
                    Category = collection.Name,
                    IsPaginated = true,
                    CurrentPage = pageResult.CurrentPage,
                    TotalPageCount = pageResult.TotalPageCount,
                    HasPrevious = pageResult.HasPrevious,
                    HasNext = pageResult.HasNext,
                    Items = [.. pageResult.Items.Select(ToCategoryItem)]
                });
        }
        catch (Exception ex)
        {
            return Result<GetCurationCategoryV2ResponseDto?>.Failure(new("500", ex.Message));
        }
    }

    public Task<long> GetCacheVersionAsync() => settingsRepository.GetVersionAsync();

    public async Task<Result<IReadOnlyList<CurationCategoryGroupV2ResponseDto>>> GetCategoriesGroupsAsync(
        IReadOnlyList<string> categories)
    {
        try
        {
            var requestedNames = categories
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToList();

            if (requestedNames.Count == 0)
                return Result<IReadOnlyList<CurationCategoryGroupV2ResponseDto>>.Success([]);

            var collectionsResult = await collectionService.GetActiveByNamesAsync(requestedNames);

            if (collectionsResult.IsFailure)
                return Result<IReadOnlyList<CurationCategoryGroupV2ResponseDto>>.Failure(collectionsResult.Error);

            var activeCollections = collectionsResult.Data
                ?? new Dictionary<string, CurationCollectionEntity>(StringComparer.OrdinalIgnoreCase);

            var distinctCollections = requestedNames
                .Select(name => activeCollections.TryGetValue(name, out var collection) ? collection : null)
                .Where(collection => collection is not null)
                .Select(collection => collection!)
                .DistinctBy(collection => collection.Id)
                .ToList();

            var resolutions = new GroupResolution[distinctCollections.Count];

            using var semaphore = new SemaphoreSlim(CurationConstants.MaxParallelCategoryResolutions);

            await Task.WhenAll(distinctCollections.Select(async (collection, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    resolutions[index] = await ResolveGroupAsync(collection);
                }
                finally
                {
                    semaphore.Release();
                }
            }));

            var resolutionById = distinctCollections
                .Select((collection, index) => (collection.Id, Resolution: resolutions[index]))
                .ToDictionary(item => item.Id, item => item.Resolution);

            var groups = new List<CurationCategoryGroupV2ResponseDto>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in requestedNames)
            {
                if (!activeCollections.TryGetValue(name, out var collection))
                    continue;

                if (!resolutionById.TryGetValue(collection.Id, out var resolution))
                    continue;

                if (resolution.Items.Count == 0)
                    continue;

                var ordered = resolution.Items
                    .OrderBy(item => seen.Contains(ItemKey(item)))
                    .ToList();

                foreach (var item in resolution.Items)
                    seen.Add(ItemKey(item));

                groups.Add(new CurationCategoryGroupV2ResponseDto
                {
                    Category = collection.Name,
                    PageSize = resolution.Page?.PageSize,
                    CurrentPage = resolution.Page?.CurrentPage,
                    TotalPageCount = resolution.Page?.TotalPageCount,
                    HasPrevious = resolution.Page?.HasPrevious,
                    HasNext = resolution.Page?.HasNext,
                    Items = [.. ordered.Select(ToCategoryItem)]
                });
            }

            return Result<IReadOnlyList<CurationCategoryGroupV2ResponseDto>>.Success(groups);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CurationCategoryGroupV2ResponseDto>>.Failure(new("500", ex.Message));
        }
    }

    private async Task<GroupResolution> ResolveGroupAsync(CurationCollectionEntity collection)
    {
        if (collection.Mode == ECurationCollectionMode.DYNAMIC && collection.AllowsPagination)
        {
            var page = await collectionResolver.ResolvePageAsync(collection, page: 1, pageSize: 0);
            return new(page.Items, page);
        }

        var items = await collectionResolver.ResolveItemsAsync(collection);
        return new(items, null);
    }

    private sealed record GroupResolution(
        IReadOnlyList<ResolvedCurationContent> Items,
        CurationPageResult? Page);

    private static string ItemKey(ResolvedCurationContent item) =>
        $"{item.ContentType}:{item.ContentId}";

    private FeaturedContent ToFeaturedContent(ResolvedCurationContent item)
    {
        return item.ContentType == EContentType.MOVIE
            ? new FeaturedContent(
                MovieContentV2ResponseDto.FromEntity(item.Movie!, EncryptionKey), "movie")
            : new FeaturedContent(
                SeriesSummaryContentV2ResponseDto.FromEntity(item.Series!), "series");
    }

    private object ToCategoryItem(ResolvedCurationContent item)
    {
        return item.ContentType == EContentType.MOVIE
            ? MovieContentV2ResponseDto.FromEntity(item.Movie!, EncryptionKey)
            : SeriesSummaryContentV2ResponseDto.FromEntity(item.Series!);
    }

    private string EncryptionKey =>
        configuration["SecuritySettings:ContentEncryptionKey"]!;
}
