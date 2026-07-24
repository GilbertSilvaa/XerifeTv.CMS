using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public sealed class ImportSpreadsheetBackgroundJobProcessorStrategy : IBackgroundJobProcessorStrategy
{
    private readonly IServiceProvider _serviceProvider;

    public ImportSpreadsheetBackgroundJobProcessorStrategy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job)
    {
        using var scope = _serviceProvider.CreateScope();
        var backgroundJobQueueService = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();

        Type serviceType = typeof(IMovieService);

        if (job.Type == EBackgroundJobType.REGISTER_SPREADSHEET_SERIES)
            serviceType = typeof(ISeriesService);

        else if (job.Type == EBackgroundJobType.REGISTER_SPREADSHEET_CHANNELS)
            serviceType = typeof(IChannelService);

        var importerType = typeof(ISpreadsheetBatchImporter<>).MakeGenericType(serviceType);

        var spreadsheetBatchImporter = (ISpreadsheetBatchImporter)scope.ServiceProvider.GetRequiredService(importerType);

        var file = await DownloadExcelAsFormFileAsync(job.SpreadsheetFileUrl!);
        var importResult = await spreadsheetBatchImporter.ImportAsync(file);

        if (importResult.IsFailure || string.IsNullOrEmpty(importResult.Data))
            return;

        var importId = importResult.Data;

        while (true)
        {
            var monitorResult = await spreadsheetBatchImporter.MonitorImportAsync(importId);

            if (monitorResult.IsFailure || monitorResult.Data == null) continue;

            var data = monitorResult.Data;

            var updateBackgroundJobDto = new UpdateBackgroundJobRequestDto
            {
                Id = job.Id,
                TotalRecordsToProcess = data.TotalItemsCount ?? 0,
                TotalFailedRecords = data.FailCount ?? 0,
                TotalSuccessfulRecords = data.SuccessCount ?? 0,
                TotalProcessedRecords = data.ProcessedCount ?? 0,
                ErrorList = data.ErrorList,
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
        => jobType is EBackgroundJobType.REGISTER_SPREADSHEET_MOVIES or
           EBackgroundJobType.REGISTER_SPREADSHEET_SERIES or
           EBackgroundJobType.REGISTER_SPREADSHEET_CHANNELS;

    private static async Task<IFormFile> DownloadExcelAsFormFileAsync(string fileUrl, string fileName = "_arquivo.xlsx")
    {
        using var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);

        var stream = new MemoryStream(fileBytes);
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        return formFile;
    }
}
