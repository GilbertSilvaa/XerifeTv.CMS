using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;

namespace XerifeTv.CMS.Modules.ContentCuration.Services;

public sealed class CurationSnapshotScheduler(
    ICurationCollectionSnapshotRepository snapshotRepository,
    IBackgroundJobQueueService backgroundJobQueueService,
    ICacheService cacheService) : ICurationSnapshotScheduler
{
    private const string CatalogRebuildCacheKey = "curation_snapshots_catalog_rebuild_requested";

    private static readonly TimeSpan CatalogRebuildDebounce = TimeSpan.FromSeconds(30);

    public async Task ScheduleRebuildAsync(string collectionId, string collectionName)
    {
        await snapshotRepository.DeleteByCollectionIdAsync(collectionId);

        await backgroundJobQueueService
            .AddCurationSnapshotRebuildJobAsync(collectionId, collectionName);
    }

    public Task RemoveSnapshotAsync(string collectionId)
        => snapshotRepository.DeleteByCollectionIdAsync(collectionId);

    public async Task ScheduleCatalogRebuildAsync()
    {
        if (await cacheService.GetValueAsync<bool>(CatalogRebuildCacheKey))
            return;

        await cacheService.SetValueAsync(CatalogRebuildCacheKey, CatalogRebuildDebounce, true);

        var result = await backgroundJobQueueService
            .AddCurationSnapshotRebuildJobAsync(
                curationCollectionId: null,
                curationCollectionName: null);

        if (result.IsFailure)
            await cacheService.RemoveAsync(CatalogRebuildCacheKey);
    }
}
