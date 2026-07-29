using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public sealed class ImportEpisodesSeriesBackgroundJobProcessorStrategy(
    IServiceProvider serviceProvider) : IBackgroundJobProcessorStrategy
{
    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job)
    {
        using var scope = serviceProvider.CreateScope();
        var episodesImporter = scope.ServiceProvider.GetRequiredService<IEpisodesImporter>();
        var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();

        var importResult = await episodesImporter.ImportAsync(job.SeriesIdImportEpisodes!);

        if (importResult.IsFailure || string.IsNullOrEmpty(importResult.Data)) return;
        var importId = importResult.Data;

        while (true)
        {
            var monitorResult = await episodesImporter.MonitorImportAsync(importId);

            if (monitorResult.IsFailure || monitorResult.Data == null) continue;

            var data = monitorResult.Data;

            var updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = data.TotalItemsCount,
                TotalSuccessfulRecords = data.ImportedCount,
                TotalProcessedRecords = data.ProcessedCount,
                Status = EBackgroundJobStatus.PROCESSING
            };

            if (data.ProgressCount == 100)
            {
                updateBackgroundJobDto.Status =
                    updateBackgroundJobDto.TotalFailedRecords == updateBackgroundJobDto.TotalRecordsToProcess
                    ? EBackgroundJobStatus.FAILED
                    : EBackgroundJobStatus.COMPLETED;

                await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);

                break;
            }

            await backgroundJobQueueService.UpdateAsync(updateBackgroundJobDto);
        }
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType == EBackgroundJobType.IMPORT_EPISODES_FROM_SERIES_IMDB;
}
