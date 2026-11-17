using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Models;

namespace XerifeTv.CMS.Modules.ContentCuration.Services;

public sealed class CurationSnapshotService(
    ICurationCollectionRepository collectionRepository,
    ICurationCollectionSnapshotRepository snapshotRepository,
    ICurationCollectionResolver collectionResolver,
    ICurationSettingsRepository settingsRepository) : ICurationSnapshotService
{
    public async Task<CurationSnapshotSlice?> GetSliceAsync(
        CurationCollectionEntity collection, int page, int pageSize)
    {
        if (collection.Mode != ECurationCollectionMode.DYNAMIC)
            return null;

        var limit = ResolveLimit(collection);

        if (!collection.AllowsPagination)
        {
            var snapshot = await snapshotRepository.GetByCollectionIdAsync(collection.Id);

            if (snapshot is null)
                return null;

            var items = snapshot.Items.Take(limit).ToList();
            return new(items, snapshot.TotalCount);
        }

        var size = pageSize > 0 ? pageSize : limit;
        var currentPage = Math.Max(1, page);
        var skip = (long)(currentPage - 1) * size;
        var skipValue = (int)Math.Min(int.MaxValue, skip);

        var pageSnapshot = await snapshotRepository.GetPageAsync(collection.Id, skipValue, size);

        if (pageSnapshot is null)
            return null;

        return new(pageSnapshot.Items, pageSnapshot.TotalCount);
    }

    public async Task<IReadOnlyDictionary<string, CurationSnapshotSlice>> GetFirstSlicesAsync(
        IReadOnlyList<CurationCollectionEntity> collections)
    {
        var dynamicCollections = collections
            .Where(collection => collection.Mode == ECurationCollectionMode.DYNAMIC)
            .ToList();

        var result = new Dictionary<string, CurationSnapshotSlice>(StringComparer.Ordinal);

        if (dynamicCollections.Count == 0)
            return result;

        var snapshots = await snapshotRepository
            .GetFirstPageByCollectionIdsAsync(dynamicCollections.Select(collection => collection.Id));

        var snapshotById = snapshots
            .GroupBy(snapshot => snapshot.CollectionId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var collection in dynamicCollections)
        {
            if (!snapshotById.TryGetValue(collection.Id, out var snapshot))
                continue;

            var limit = ResolveLimit(collection);

            result[collection.Id] = new CurationSnapshotSlice(
                snapshot.Items.Take(limit).ToList(),
                snapshot.TotalCount);
        }

        return result;
    }

    public async Task RebuildAsync(
        CurationCollectionEntity collection,
        CancellationToken cancellationToken = default)
    {
        if (collection.Mode != ECurationCollectionMode.DYNAMIC)
        {
            await snapshotRepository.DeleteByCollectionIdAsync(collection.Id);
            return;
        }

        var items = await CollectItemsAsync(collection, cancellationToken);
        var version = await settingsRepository.GetVersionAsync();

        var snapshot = new CurationCollectionSnapshotEntity
        {
            CollectionId = collection.Id,
            CollectionName = collection.Name,
            AllowsPagination = collection.AllowsPagination,
            PageSize = ResolveLimit(collection),
            TotalCount = items.Count,
            CurationVersion = version,
            Items = items
        };

        await snapshotRepository.UpsertAsync(snapshot);
    }

    public async Task<int> RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        var collections = await collectionRepository.GetOrderedAsync(onlyActive: true);

        var dynamicCollections = collections
            .Where(collection => collection.Mode == ECurationCollectionMode.DYNAMIC)
            .ToList();

        foreach (var collection in dynamicCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RebuildAsync(collection, cancellationToken);
        }

        await RemoveStaleSnapshotsAsync(dynamicCollections);

        return dynamicCollections.Count;
    }

    public Task RemoveAsync(string collectionId)
        => snapshotRepository.DeleteByCollectionIdAsync(collectionId);

    private async Task<List<CurationSnapshotItem>> CollectItemsAsync(
        CurationCollectionEntity collection,
        CancellationToken cancellationToken)
    {
        var items = collection.AllowsPagination
            ? await collectionResolver.ResolveAllAsync(collection, cancellationToken)
            : await collectionResolver.ResolveItemsAsync(collection);

        return items.Select(ToSnapshotItem).ToList();
    }

    private async Task RemoveStaleSnapshotsAsync(
        IReadOnlyList<CurationCollectionEntity> activeDynamicCollections)
    {
        var snapshotIds = await snapshotRepository.GetAllCollectionIdsAsync();

        if (snapshotIds.Count == 0) return;

        var activeIds = activeDynamicCollections
            .Select(collection => collection.Id)
            .ToHashSet(StringComparer.Ordinal);

        var staleIds = snapshotIds
            .Where(id => !activeIds.Contains(id))
            .ToList();

        await snapshotRepository.DeleteManyByCollectionIdsAsync(staleIds);
    }

    private static int ResolveLimit(CurationCollectionEntity collection)
        => collection.Limit > 0 ? collection.Limit : CurationConstants.DefaultCollectionLimit;

    private static CurationSnapshotItem ToSnapshotItem(ResolvedCurationContent content)
        => new()
        {
            ContentId = content.ContentId,
            ContentType = content.ContentType
        };
}
