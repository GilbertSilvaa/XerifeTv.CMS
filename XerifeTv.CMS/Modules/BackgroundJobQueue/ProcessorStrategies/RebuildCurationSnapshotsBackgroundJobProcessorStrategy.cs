using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public sealed class RebuildCurationSnapshotsBackgroundJobProcessorStrategy(
    IServiceProvider serviceProvider) : IBackgroundJobProcessorStrategy
{
    public bool CanProcess(EBackgroundJobType jobType)
        => jobType == EBackgroundJobType.REBUILD_CURATION_COLLECTION_SNAPSHOTS;

    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job, CancellationToken cancellation)
    {
        using var scope = serviceProvider.CreateScope();

        var backgroundJobQueueService = scope.ServiceProvider
            .GetRequiredService<IBackgroundJobQueueService>();
        var collectionRepository = scope.ServiceProvider
            .GetRequiredService<ICurationCollectionRepository>();
        var snapshotService = scope.ServiceProvider
            .GetRequiredService<ICurationSnapshotService>();

        await backgroundJobQueueService.UpdateAsync(new UpdateBackgroundJobRequestDto
        {
            Id = job.Id,
            Status = EBackgroundJobStatus.PROCESSING,
            TotalRecordsToProcess = 1
        });

        try
        {
            var processed = 0;

            if (!string.IsNullOrWhiteSpace(job.CurationCollectionId))
            {
                var collection = await collectionRepository.GetAsync(job.CurationCollectionId);

                if (collection is null)
                    await snapshotService.RemoveAsync(job.CurationCollectionId);
                else
                    await snapshotService.RebuildAsync(collection, cancellation);

                processed = 1;
            }
            else
            {
                processed = await snapshotService.RebuildAllAsync(cancellation);
            }

            await backgroundJobQueueService.UpdateAsync(new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                Status = EBackgroundJobStatus.COMPLETED,
                TotalRecordsToProcess = processed,
                TotalSuccessfulRecords = processed,
                TotalProcessedRecords = processed
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await backgroundJobQueueService.UpdateAsync(new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                Status = EBackgroundJobStatus.FAILED,
                TotalFailedRecords = 1,
                TotalProcessedRecords = 1,
                ErrorList = [ex.InnerException?.Message ?? ex.Message]
            });
        }
    }
}
