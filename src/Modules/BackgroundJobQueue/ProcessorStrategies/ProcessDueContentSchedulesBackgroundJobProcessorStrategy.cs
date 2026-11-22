using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Services;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;

public sealed class ProcessDueContentSchedulesBackgroundJobProcessorStrategy(
    IServiceProvider serviceProvider) : IBackgroundJobProcessorStrategy
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(30);
    private const int BatchSize = 100;

    public async Task ProcessJobAsync(GetBackgroundJobResponseDto job, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IContentScheduleRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundJobQueueService>();

        await repository.ReleaseStaleClaimsAsync(ClaimLease);
        var schedules = await repository.GetDueUnnotifiedAsync(clock.Today, BatchSize);
        var processed = 0;
        var failed = 0;

        foreach (var schedule in schedules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimed = await repository.TryClaimAsync(schedule.Id, ClaimLease);
            if (claimed is null)
                continue;

            var dispatch = await queue.AddJobInQueueAsync(
                EDispatchWebhooksJobQueueType.CONTENT_SCHEDULE, claimed.Id);

            if (dispatch.IsSuccess)
            {
                await repository.MarkProcessedAsync(claimed.Id);
                processed++;
            }
            else
            {
                await repository.ReleaseClaimAsync(claimed.Id);
                failed++;
            }
        }

        await queue.UpdateAsync(new UpdateBackgroundJobRequestDto
        {
            Id = job.Id,
            TotalRecordsToProcess = schedules.Count,
            TotalProcessedRecords = processed + failed,
            TotalSuccessfulRecords = processed,
            TotalFailedRecords = failed,
            Status = failed == schedules.Count && schedules.Count > 0
                ? EBackgroundJobStatus.FAILED
                : EBackgroundJobStatus.COMPLETED
        });
    }

    public bool CanProcess(EBackgroundJobType jobType)
        => jobType == EBackgroundJobType.PROCESS_DUE_CONTENT_SCHEDULES;
}
