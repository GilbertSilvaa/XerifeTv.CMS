using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;

namespace XerifeTv.CMS.Modules.ContentCuration;

public sealed class CurationSnapshotInitializer(
    IServiceProvider serviceProvider,
    ILogger<CurationSnapshotInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var collectionRepository = scope.ServiceProvider
                .GetRequiredService<ICurationCollectionRepository>();
            var snapshotRepository = scope.ServiceProvider
                .GetRequiredService<ICurationCollectionSnapshotRepository>();
            var backgroundJobQueueService = scope.ServiceProvider
                .GetRequiredService<IBackgroundJobQueueService>();

            var dynamicCollections = (await collectionRepository.GetOrderedAsync(onlyActive: true))
                .Where(collection => collection.Mode == ECurationCollectionMode.DYNAMIC)
                .ToList();

            if (dynamicCollections.Count == 0) return;

            var snapshotIds = (await snapshotRepository.GetAllCollectionIdsAsync())
                .ToHashSet(StringComparer.Ordinal);

            var allSnapshotsExist = dynamicCollections
                .All(collection => snapshotIds.Contains(collection.Id));

            if (allSnapshotsExist) return;

            await backgroundJobQueueService
                .AddCurationSnapshotRebuildJobAsync(
                    curationCollectionId: null,
                    curationCollectionName: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to schedule the initial curation snapshot rebuild");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
